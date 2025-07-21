using AndroidDeviceVerificationTool.Controls;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AndroidDeviceVerificationTool.Design.MaterialDesign;
using static System.Net.Mime.MediaTypeNames;



namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// 🎯 모든 MQTT 기능 통합 매니저 (PC ↔ Android 양방향)
    /// </summary>
    public class UnifiedMqttManager
    {
        private static UnifiedMqttManager? _instance;
        public static UnifiedMqttManager Instance => _instance ??= new UnifiedMqttManager();
        
        // 🎯 SharedApplicationData 참조 (중앙 관리)
        private readonly SharedApplicationData sharedData;
        
        // 📡 기본 MQTT 처리 (시뮬레이션용)
        private readonly MqttMessageManager mqttCore;
        
        // 🌐 실제 MQTT 처리
        private readonly RealMqttManager realMqtt;
        
        // 📤 대기 중인 요청들 (PC → Android await용)
        private readonly ConcurrentDictionary<string, TaskCompletionSource<MqttResponse>> waitingRequests = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<MqttRes>> waitingRequests2 = new();

        // 📥 이벤트 핸들러들 (Android → PC 처리용)
        private readonly Dictionary<string, Func<MqttCommand, Task<MqttResponse>>> eventHandlers = new();
        
        private UnifiedMqttManager()
        {
            Console.WriteLine("UnifiedMqttManager");
            sharedData = SharedApplicationData.Instance;
            mqttCore = new MqttMessageManager();
            realMqtt = new RealMqttManager();
            
            // 🔗 MQTT 메시지 수신 시 자동으로 라우팅
            mqttCore.MessageReceived += OnMqttMessageReceived;
            realMqtt.MessageReceived += OnRealMqttMessageReceived;
            
            // 📋 기본 이벤트 핸들러들 등록
            RegisterDefaultHandlers();
        }

        #region 📤 PC → Android 통신

        public static string GenerateNanoId(int length = 8)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[random.Next(s.Length)])
                                        .ToArray());
        }

        public static string GetCurrentTimestamp()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss");
        }

        //private Action<int>? onStartButtonEnableCallback;
        //public void RegisterStartButtonCallback(Action<int> callback)
        //{
        //    onStartButtonEnableCallback = callback;
        //}

        /// <summary>
        /// ✅ PC에서 Android로 명령 보내고 응답 기다리기 (AWAIT)
        /// </summary>
        public async Task<MqttResponse> SendAndWait(string deviceId, string command, object data = null)
        {
            Console.WriteLine("SendAndWait");
            string requestId = Guid.NewGuid().ToString();
            
            // 🎮 Test Mode: 가짜 응답 즉시 반환
            //if (true) // 현재는 항상 Test Mode
            //{
            //    return await SimulateAndroidResponse(deviceId, command, data);
            //}
            
            var tcs = new TaskCompletionSource<MqttResponse>();
            
            // 응답 대기 등록
            waitingRequests[requestId] = tcs;
            
            try
            {
                // MQTT 전송 (Test 모드 vs Production 모드)
                var message = new { requestId, deviceId, command, data, timestamp = DateTime.Now };
                string messageJson = JsonSerializer.Serialize(message);
                
                bool sendSuccess;
                if (false) // Test Mode
                {
                    sendSuccess = await mqttCore.SendMessageAsync($"device/{deviceId}/command", messageJson);
                }
                else
                {
                    //sendSuccess = await realMqtt.PublishAsync($"device/{deviceId}/command", messageJson);
                    sendSuccess = await realMqtt.PublishAsync($"{deviceId}", messageJson);
                }

                if (!sendSuccess)
                {
                    waitingRequests.TryRemove(requestId, out _);
                    return new MqttResponse { Status = "ERROR", Message = "메시지 전송 실패" };
                }
                
                // 응답 대기 (30초 타임아웃)
                var timeout = Task.Delay(30000);
                var completed = await Task.WhenAny(tcs.Task, timeout);
                
                if (completed == timeout)
                {
                    waitingRequests.TryRemove(requestId, out _);
                    return new MqttResponse { Status = "TIMEOUT", Message = "응답 시간 초과" };
                }
                
                return await tcs.Task;
            }
            catch (Exception ex)
            {
                waitingRequests.TryRemove(requestId, out _);
                return new MqttResponse { Status = "ERROR", Message = ex.Message };
            }
        }
        
        /// <summary>
        /// 🔥 PC에서 Android로 명령 보내기 (FIRE-AND-FORGET)
        /// ⚡ 즉시 반환! await 없이 호출하세요!
        /// </summary>
        public void SendFireAndForget(string deviceId, string command, object data = null)
        {
            Console.WriteLine("SendFireAndForget");
            // 🎮 Test Mode: 로컬 핸들러 직접 호출
            if (true) // Test Mode
            {
                _ = Task.Run(async () => await SimulateLocalEventHandling(deviceId, command, data));
                return;
            }
            
            // 🔥 백그라운드에서 전송 - 호출자는 즉시 완료
            _ = Task.Run(async () =>
            {
                try
                {
                    var message = new { deviceId, command, data, timestamp = DateTime.Now, fireAndForget = true };
                    string messageJson = JsonSerializer.Serialize(message);
                    
                    if (true) // Test Mode (시뮬레이션)
                    {
                        await mqttCore.SendMessageAsync($"device/{deviceId}/command", messageJson);
                    }
                    else
                    {
                        await realMqtt.PublishAsync($"device/{deviceId}/command", messageJson);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔥 Fire-and-Forget 전송 실패: {ex.Message}");
                    // Fire-and-Forget이므로 에러도 무시
                }
            });
        }
        
        #endregion
        
        #region 📥 Android → PC 통신
        
        /// <summary>
        /// 📋 이벤트 핸들러 등록 (Android → PC 처리용)
        /// </summary>
        public void RegisterHandler(string command, Func<MqttCommand, Task<MqttResponse>> handler)
        {
            eventHandlers[command] = handler;
        }
        
        /// <summary>
        /// 📋 간단한 핸들러 등록 (동기 버전)
        /// </summary>
        public void RegisterHandler(string command, Func<MqttCommand, MqttResponse> handler)
        {
            eventHandlers[command] = async (cmd) => await Task.FromResult(handler(cmd));
        }
        
        /// <summary>
        /// 📋 액션만 실행하는 핸들러 (응답 없음)
        /// </summary>
        public void RegisterHandler(string command, Action<MqttCommand> handler)
        {
            eventHandlers[command] = async (cmd) => {
                handler(cmd);
                return await Task.FromResult(new MqttResponse { Status = "OK" });
            };
        }
        
        #endregion
        
        #region 🔗 내부 처리
        
        /// <summary>
        /// 📨 MQTT 메시지 수신 처리 (자동 라우팅) - 시뮬레이션용
        /// </summary>
        private async void OnMqttMessageReceived(int deviceIndex, string topic, string payload)
        {
            Console.WriteLine($"OnMqttMessageReceived. [{topic}] {payload}");
            try
            {
                if (topic.Contains("/response") || topic.Contains("/result"))
                {
                    // 📥 Android → PC 응답 (await 완료시키기)
                    HandleAndroidResponse(payload);
                }
                else if (topic.Contains("/command") || topic.Contains("pc/command"))
                {
                    // 📥 Android → PC 명령 (핸들러로 처리)
                    await HandleAndroidCommand(payload);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MQTT 메시지 처리 오류: {ex.Message}");
            }
        }


        private async void recv_msg_proc(UnifiedDeviceData device, string payload)
        {
            try
            {
                var res = JsonSerializer.Deserialize<MqttRes>(payload);
                if (res != null && res.Res != null)
                {
                    //Console.WriteLine($"recv_msg_proc1: {res.Res}, {res.Result}, {res.Uid}, {res.Date}");
                    //Console.WriteLine("recv_msg_proc2:" + res.Params);

                    switch (res.Res)
                    {
                        case "dev_join_res":
                            {
                                //device.p
                                Console.WriteLine("dev_join_res======================================");
                                //onStartButtonEnableCallback?.Invoke(0);

                            }
                            break;
                    }

                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"recv_msg_proc. error: {e.Message}");
            }
        }

        public enum TestType
        {
            Auto,
            Manual,
            Unknown
        }

        public static TestType GetTestType(string? testItem)
        {
            if (string.IsNullOrEmpty(testItem))
                return TestType.Unknown;

            if (testItem.StartsWith("auto_", StringComparison.OrdinalIgnoreCase))
                return TestType.Auto;

            if (testItem.StartsWith("manual_", StringComparison.OrdinalIgnoreCase))
                return TestType.Manual;

            return TestType.Unknown;
        }

        /// <summary>
        /// 📨 실제 MQTT 메시지 수신 처리 (자동 라우팅)
        /// </summary>
        private async void OnRealMqttMessageReceived(string topic, string payload)
        {
            Console.WriteLine($"[MQTT-Recv] [{topic}] {payload}");
            try
            {
                bool ret = HandleMqttResponse(payload);

                //if (ret)
                {
                    //recv_msg_proc(payload);
                }

                var res = JsonSerializer.Deserialize<MqttRes>(payload);

                var device = sharedData.GetDeviceBySubTopic(topic);
                if(device == null)
                {
                    Console.WriteLine("Allocated Device NOT found. drop");
                    return;
                }

                if (res.Res.Contains("do_test_") == false) {
                    //do_test_req를 제외한 다른 메시지는 recv_msg_proc에서 처리함.
                    recv_msg_proc(device, payload);
                    return;
                }

                Console.WriteLine("deviceID:" + device.DeviceId);
                //Console.WriteLine("COUNT:" + device.PeripheralData.ManualTests.Count);
                var dj = new DynamicJson(res.Params.ToString());
                if (dj.IsValid!= true)
                {
                    Console.WriteLine("res.Params parsing failed:"+ res.Params.ToString());
                    return;
                }

                //Console.WriteLine("reult:" + res.Result);
                //Console.WriteLine("params:" + res.Params.ToString());
                string? testItem = dj.GetString("test_item");
                //Console.WriteLine("test_item:" + testItem);
                var testType = GetTestType(testItem);

                if (testType == TestType.Auto)
                {
                    int ac = device.PeripheralData.AutoTests.Count;
                    for ( int i=0; i<ac; i++)
                    {
                        if (testItem.Contains(device.PeripheralData.AutoTests[i].Name))
                        {
                            UnifiedTestItem test = device.PeripheralData.AutoTests[i];
                            test.Params = res.Params.ToString();
                            if( test.CompletionSource!=null && !test.CompletionSource.Task.IsCompleted)
                            {
                                var testResult = new FunctionTestResult
                                {
                                    Description = "응답수신완료",
                                    Result = res.Result
                                };
                                test.CompletionSource.SetResult(testResult);
                                test.CompletionSource = null;
                            }
                            Console.WriteLine($"AutoTEST: {res.Res}, {res.Result}");
                            //Console.WriteLine($"AutoTEST_____: {device.PeripheralData.AutoTests[i].TestFunction.Description}");

                            device.PeripheralData.AutoTests[i].Params = res.Params.ToString();
                            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!");
                            Console.WriteLine($"[AutoTEST,{i}] {device.PeripheralData.AutoTests[i].Name}, {device.PeripheralData.AutoTests[i].Params.ToString()}");
                        }
                    }
                }
                else if(testType == TestType.Manual)
                {
                    int ac = device.PeripheralData.ManualTests.Count;
                    for (int i = 0; i < ac; i++)
                    {
                        if (testItem.Contains(device.PeripheralData.ManualTests[i].Name))
                        {
                            UnifiedTestItem test = device.PeripheralData.ManualTests[i];
                            test.Params = res.Params.ToString();
                            if (test.CompletionSource != null && !test.CompletionSource.Task.IsCompleted)
                            {
                                var testResult = new FunctionTestResult
                                {
                                    Description = "응답수신완료",
                                    Result = res.Result
                                };
                                test.CompletionSource.SetResult(testResult);
                                test.CompletionSource = null;
                            }
                            Console.WriteLine($"ManualTEST: {res.Res}, {res.Result}");


                            device.PeripheralData.ManualTests[i].Params = res.Params.ToString();
                            Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!");
                            Console.WriteLine($"[ManualTEST,{i}] {device.PeripheralData.ManualTests[i].Name}, {device.PeripheralData.ManualTests[i].Params.ToString()}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"ERROR??: test_item: {testItem}");
                }
                //Console.WriteLine("test_item:" + res.Params);




                if (device != null && device.PeripheralData?.ManualTests != null && device.PeripheralData.ManualTests.Count > 2)
                {

                    device.PeripheralData.ManualTests[2].Params = res.Params.ToString();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 실제 MQTT 메시지 처리 오류: {ex.Message}");
            }
        }



        /*
                /// <summary>
                /// 📥 Android 응답 처리 (PC await 완료)
                /// </summary>
                private void HandleAndroidResponse(string payload)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(payload))
                        {
                            Console.WriteLine("⚠️ 빈 페이로드, 스킵");
                            return;
                        }

                        // 🔍 페이로드 상세 디버깅
                        //Console.WriteLine($"📄 페이로드 길이: {payload.Length}");
                        //Console.WriteLine($"📄 첫 10자: '{payload.Substring(0, Math.Min(10, payload.Length))}'");
                        //Console.WriteLine($"📄 첫 문자 코드: {(int)payload[0]}");
                        Console.WriteLine($"Received: {payload}");

                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = JsonCommentHandling.Skip
                        };

                        var response = JsonSerializer.Deserialize<MqttResponse>(payload, options);

                        if (response?.RequestId != null && waitingRequests.TryRemove(response.RequestId, out var tcs))
                        {
                            tcs.SetResult(response);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ JSON 파싱 실패: {ex.Message}");
                    }
                }
        */

        /// <summary>
        /// 📥 Android 응답 처리 (PC await 완료)
        /// </summary>
        private void HandleAndroidResponse(string payload)
        {
            //Console.WriteLine("HandleAndroidResponse");
            try
            {
                //Console.WriteLine($"payload: {payload}");
                var response = JsonSerializer.Deserialize<MqttResponse>(payload); 
                //Console.WriteLine($"HandleAndroidResponse: {response}");

                Console.WriteLine($"HandleAndroidResponse/ response.requestId: {response.requestId}");

                if (response?.requestId != null && waitingRequests.TryRemove(response.requestId, out var tcs))
                {
                    Console.WriteLine("old request removed");
                    tcs.SetResult(response);
                }
                else
                {
                    Console.WriteLine("msg dropped. response: "+ response);
                }
            }
            catch
            { /* 무시 */
                Console.WriteLine("ERROR..??");
            }
        }

        private bool HandleMqttResponse(string payload)
        {
            //Console.WriteLine("HandleMqttResponse");
            try
            {
                //Console.WriteLine($"payload: {payload}");
                var res = JsonSerializer.Deserialize<MqttRes>(payload);
                //Console.WriteLine($"HandleAndroidResponse: {res}");

                //Console.WriteLine($"HandleMqttResponse/ response.uid: {res.Uid}");

                if (res?.Uid != null && waitingRequests2.TryRemove(res.Uid, out var tcs))
                {
                    Console.WriteLine("HandleMqttResponse/old request removed");
                    tcs.SetResult(res);
                    return true;
                }
                else
                {
                    Console.WriteLine("HandleMqttResponse/msg dropped. response: " + res);
                    return false;
                }
            }
            catch
            { /* 무시 */
                Console.WriteLine("HandleMqttResponse/ERROR..??");
                return false;
            }
        }


        /// <summary>
        /// 📥 Android 명령 처리 (이벤트 핸들러 실행)
        /// </summary>
        private async Task HandleAndroidCommand(string payload)
        {
            Console.WriteLine("HandleAndroidCommand");
            try
            {
                var command = JsonSerializer.Deserialize<MqttCommand>(payload);
                
                if (eventHandlers.TryGetValue(command.Command, out var handler))
                {
                    var response = await handler(command);
                    response.RequestId = command.RequestId;
                    
                    // 응답이 필요한 경우만 전송
                    if (!string.IsNullOrEmpty(command.RequestId))
                    {
                        await mqttCore.SendMessageAsync($"device/{command.DeviceId}/response", 
                            JsonSerializer.Serialize(response));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Android 명령 처리 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 📋 기본 이벤트 핸들러들 자동 등록
        /// </summary>
        private void RegisterDefaultHandlers()
        {
            Console.WriteLine("RegisterDefaultHandlers");
            var sharedData = SharedApplicationData.Instance;
            
            // 🔌 디바이스 연결 알림
            RegisterHandler("device_connected", (cmd) => {
                var deviceIndex = FindOrCreateDevice(cmd.DeviceId);
                sharedData.ConnectDevice(deviceIndex, cmd.DeviceId);
                sharedData.AddDeviceLog(deviceIndex, "Android Device Auto Connected!", "SUCCESS");
                Console.WriteLine($"{deviceIndex}, Android Device Auto Connected!");

                return new MqttResponse { Status = "OK", Message = "연결 확인" };
            });
            
            // ⚠️ 에러 알림  
            RegisterHandler("device_error", (cmd) => {
                var deviceIndex = FindOrCreateDevice(cmd.DeviceId);
                sharedData.AddDeviceLog(deviceIndex, $"❌ Android 에러: {cmd.Data}", "ERROR");
                Console.WriteLine($"{deviceIndex}, Android Error: {cmd.Data}");
                return new MqttResponse { Status = "OK", Message = "에러 확인" };
            });
            
            // 📊 상태 변경
            RegisterHandler("status_changed", (cmd) => {
                var deviceIndex = FindOrCreateDevice(cmd.DeviceId);
                sharedData.UpdateTestStatus(deviceIndex, 0, cmd.Data?.ToString() ?? "UNKNOWN");
                sharedData.AddDeviceLog(deviceIndex, $"📊 상태 변경: {cmd.Data}", "INFO");
                Console.WriteLine($"{deviceIndex}, 상태 변경: {cmd.Data}");
                return new MqttResponse { Status = "OK", Message = "상태 변경 확인" };
            });
            
            // 💓 하트비트 (조용히 처리)
            RegisterHandler("heartbeat", (cmd) => {
                // 로그 스팸 방지 - 조용히 처리
                return new MqttResponse { Status = "OK", Message = "alive" };
            });
            
            // 🧪 테스트 완료 보고
            RegisterHandler("test_completed", (cmd) => {
                var deviceIndex = FindOrCreateDevice(cmd.DeviceId);
                sharedData.AddDeviceLog(deviceIndex, "🧪 Android 테스트 완료 보고됨", "SUCCESS");
                Console.WriteLine($"{deviceIndex}, Android 테스트 완료 보고됨");
                return new MqttResponse { Status = "OK", Message = "테스트 완료 확인" };
            });
            
            // 🔋 배터리 상태 (예시)
            RegisterHandler("battery_changed", (cmd) => {
                var deviceIndex = FindOrCreateDevice(cmd.DeviceId);
                sharedData.AddDeviceLog(deviceIndex, $"🔋 배터리: {cmd.Data}", "INFO");
                return new MqttResponse { Status = "OK", Message = "배터리 정보 확인" };
            });
        }
        
        /// <summary>
        /// 🔍 디바이스 찾기 또는 생성
        /// </summary>
        private int FindOrCreateDevice(string deviceId)
        {
            Console.WriteLine("FindOrCreateDevice");
            var sharedData = SharedApplicationData.Instance;
            
            // 기존 디바이스 찾기
            for (int i = 0; i < sharedData.Devices.Count; i++)
            {
                if (sharedData.Devices[i].DeviceId == deviceId)
                    return i;
            }
            
            // 새 디바이스 생성
            sharedData.AddDevice($"VD #{sharedData.Devices.Count + 1:D2}");
            return sharedData.Devices.Count - 1;
        }
        
        #endregion
        
        #region 🧪 간편 사용 메서드들
        
        /// <summary>
        /// 🧪 테스트 실행 (간편 버전)
        /// </summary>
        public async Task<string> RunTest(string deviceId, string testType)
        {
            var result = await SendAndWait(deviceId, $"test_{testType}");
            return result.Status; // "PASS", "FAIL", "TIMEOUT"
        }
        
        /// <summary>
        /// 🔥 명령 전송 (간편 버전) - Fire-and-Forget
        /// </summary>
        public void SendCommand(string deviceId, string command, object data = null)
        {
            Console.WriteLine("SendCommand");
            SendFireAndForget(deviceId, command, data);
        }
        
        #endregion
        
        #region 🎮 시뮬레이션 모드 메서드들
        
        /// <summary>
        /// 🎮 Android 응답 시뮬레이션 (SendAndWait용)
        /// </summary>
        private async Task<MqttResponse> SimulateAndroidResponse(string deviceId, string command, object data)
        {
            await Task.Delay(100); // 네트워크 딜레이 시뮬레이션
            
            // 명령에 따른 가짜 응답 생성
            return command switch
            {
                "test_camera" => new MqttResponse 
                { 
                    Status = "PASS", 
                    Message = "🎮 시뮬레이션: 카메라 테스트 성공",
                    Data = new { resolution = "1080p", fps = 30, front_camera = true }
                },
                "test_microphone" => new MqttResponse 
                { 
                    Status = "PASS", 
                    Message = "🎮 시뮬레이션: 마이크 테스트 성공",
                    Data = new { volume_level = 85, noise_reduction = true }
                },
                "get_config" => new MqttResponse 
                { 
                    Status = "OK", 
                    Message = "🎮 시뮬레이션: 설정 정보 응답",
                    Data = new 
                    { 
                        server_ip = "192.168.1.100", 
                        server_port = 8080, 
                        api_key = "ABC123XYZ",
                        test_mode = true,
                        max_retry = 3,
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                },
                _ => new MqttResponse 
                { 
                    Status = "OK", 
                    Message = $"🎮 시뮬레이션: {command} 완료",
                    Data = new { result = "success", simulated = true }
                }
            };
        }
        
        /// <summary>
        /// 🎮 로컬 이벤트 핸들러 시뮬레이션 (SendFireAndForget용)
        /// </summary>
        private async Task SimulateLocalEventHandling(string deviceId, string command, object data)
        {
            try
            {
                // 등록된 핸들러가 있으면 직접 호출
                if (eventHandlers.TryGetValue(command, out var handler))
                {
                    var mqttCommand = new MqttCommand 
                    { 
                        DeviceId = deviceId, 
                        Command = command, 
                        Data = data,
                        Timestamp = DateTime.Now
                    };
                    
                    await handler(mqttCommand);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🎮 시뮬레이션 핸들러 오류: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 🎯 디바이스 매핑 관리
        
        /// <summary>
        /// 🎯 디바이스 인덱스에 따른 토픽 가져오기
        /// </summary>
        public string GetDevicePubTopic(int deviceIndex)
        {
            var mapping = sharedData.GetSlotMapping(deviceIndex);
            return mapping.PubTopic;
            //return !string.IsNullOrEmpty(mapping.PubTopic) ? mapping.PubTopic : $"dev_{deviceIndex + 1}";
        }
        public string GetDeviceSubTopic(int deviceIndex)
        {
            var mapping = sharedData.GetSlotMapping(deviceIndex);
            return mapping.SubTopic;
        }


        /// <summary>
        /// 🎯 디바이스 인덱스에 따른 포트 가져오기
        /// </summary>
        public int GetDevicePort(int deviceIndex)
        {
            var mapping = sharedData.GetSlotMapping(deviceIndex);
            return mapping.SubPort > 0 ? mapping.SubPort : (18831 + deviceIndex);
        }
        
        /// <summary>
        /// 🎯 디바이스별 고정 매핑으로 명령 전송
        /// </summary>
        public async Task<MqttResponse> SendToDevice(int deviceIndex, string command, object data = null)
        {
            var pubT = GetDevicePubTopic(deviceIndex);
            var deviceId = $"device_{deviceIndex}";
            
            //Console.WriteLine($"🎯 디바이스 {deviceIndex} → 토픽: {topicPrefix}, 포트: {GetDevicePort(deviceIndex)}");
            Console.WriteLine($"devIndex:{deviceIndex}, pubTopic:{pubT}");

            // 🎯 고정 토픽으로 전송
            return await SendAndWaitWithTopic(pubT, deviceId, command, data);
        }


        public async Task<MqttRes> send_to_device(int deviceIndex, string devId, string cmd, object data = null)
        {
            String pubT = "";
            if (devId != null) pubT = devId;
            else pubT = GetDevicePubTopic(deviceIndex);

            //Console.WriteLine($"🎯 디바이스 {deviceIndex} → 토픽: {topicPrefix}, 포트: {GetDevicePort(deviceIndex)}");
            Console.WriteLine($"devIndex:{deviceIndex}, pubTopic:{pubT}, cmd:{cmd}");

            // 🎯 고정 토픽으로 전송
            return await send_and_wait_with_topic(pubT, cmd, data);
        }



        private async Task<MqttRes> send_and_wait_with_topic(string pubT, string cmd, object data = null)
        {
            Console.WriteLine("send_and_wait");
            //string uid = Guid.NewGuid().ToString();
            string uid = GenerateNanoId();
            string req_by = "WinPC";
            string date = GetCurrentTimestamp();

            var tcs = new TaskCompletionSource<MqttRes>();

            // 응답 대기 uid 등록
            waitingRequests2[uid] = tcs;

            string res = "do_test_res";
            try
            {
                var msg = new Dictionary<string, object>
                {
                    ["req"] = cmd,
                    ["req_by"] = req_by,
                    ["uid"] = uid,
                    ["date"] = date
                };
                if (data != null) msg["params"] = data;
                int delaySec = 10;
                if (cmd.Equals("dev_join_req")) delaySec = 3;

                if (cmd.Equals("do_test_req"))
                {
                    //Console.WriteLine($"do_test_req---------------------------------------------------------");
                    if (data is Dictionary<string, object> dict && dict.TryGetValue("test_item", out var testItem))
                    {
                        Console.WriteLine($"test_item: {testItem}");
                        if (testItem != null && testItem.Equals("auto_ap_ble")) delaySec = 15;
                        if (testItem != null && testItem.Equals("auto_ap_wifi")) delaySec = 15;
                    }
                }

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(delaySec));
                //cts.Token.Register(() => tcs.TrySetCanceled());
                cts.Token.Register(() =>
                {
                    Console.WriteLine("TIMEOUT==================");
                    Console.WriteLine("Timeout: data is null? " + (data == null));

                    if (data != null)
                    {
                        var jsonStr = JsonSerializer.Serialize(data);
                        Console.WriteLine("Serialized data: " + jsonStr);
                        //Params = JsonSerializer.Deserialize<JsonElement>(jsonStr);
                    }
                    //else
                    //{
                    //    Params = default;
                    //}

                    var timeoutRes = new MqttRes
                    {
                        Res = res,
                        Req_by = req_by,
                        Uid = uid,
                        Result = false,
                        Date = date,
                        Params = data != null ? JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(data)) : default
                    };

                    tcs.TrySetResult(timeoutRes); // ❗️취소 대신 직접 결과 설정
                });

                string jstr = JsonSerializer.Serialize(msg);
                bool sendSuccess = await realMqtt.PublishAsync(pubT, jstr);



                //Console.WriteLine("tcs.Task:"+ tcs.Task);
                //Console.WriteLine("=======================PublishAsyncPublishAsyncPublishAsyncPublishAsyncPublishAsync===========");
                return await tcs.Task;
            }
            //catch (OperationCanceledException)
            //{
            //    waitingRequests2.TryRemove(uid, out _);

            //    Console.WriteLine("TIMEOUT==================");
            //    //return new MqttRes { Result = false };
            //    return new MqttRes { Res = res, Req_by = req_by, Uid = uid, Result = false, Date = date, 
            //        Params = data!=null? JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(data)): default };
            //}
            catch (Exception ex)
            {
                waitingRequests2.TryRemove(uid, out _);
                Console.WriteLine($"ERROR================== {ex.Message}");
                return new MqttRes
                {
                    Res = res,
                    Req_by = req_by,
                    Uid = uid,
                    Result = false,
                    Date = date,
                    Params = data != null
                        ? JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(data))
                        : default
                };
            }
        }




        /// <summary>
        /// 🎯 특정 토픽으로 명령 전송하고 응답 대기
        /// </summary>
        private async Task<MqttResponse> SendAndWaitWithTopic(string topicPrefix, string deviceId, string command, object data = null)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];
            var tcs = new TaskCompletionSource<MqttResponse>();
            
            waitingRequests[requestId] = tcs;
            
            try
            {
                var message = new 
                { 
                    requestId, 
                    deviceId, 
                    command, 
                    data, 
                    timestamp = DateTime.Now 
                };
                
                string messageJson = JsonSerializer.Serialize(message);
                
                // 🎯 고정 토픽으로 발행 (예: dev_1/command)
                //string publishTopic = $"{topicPrefix}/command";
                
                        //if (false) // Test Mode (시뮬레이션)
                //{
                //    await mqttCore.SendMessageAsync(publishTopic, messageJson);
                //}
                //else
                //{
                //    await realMqtt.PublishAsync(publishTopic, messageJson);
                //}
                await realMqtt.PublishAsync(topicPrefix, messageJson);

                //Console.WriteLine($"[MQTT_send]: {publishTopic}, {messageJson}");

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                cts.Token.Register(() => tcs.TrySetCanceled());


                //Console.WriteLine("tcs.Task:"+ tcs.Task);
                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                waitingRequests.TryRemove(requestId, out _);
                return new MqttResponse { Status = "TIMEOUT", Message = "응답 시간 초과" };
            }
            catch (Exception ex)
            {
                waitingRequests.TryRemove(requestId, out _);
                return new MqttResponse { Status = "ERROR", Message = ex.Message };
            }
        }
        
        /// <summary>
        /// 🎯 디바이스별 고정 매핑으로 Fire-and-Forget 전송
        /// </summary>
        public void SendToDeviceFireAndForget(int deviceIndex, string command, object data = null)
        {
            var topicPrefix = GetDevicePubTopic(deviceIndex);
            var deviceId = $"device_{deviceIndex}";
            
            Console.WriteLine($"🔥 디바이스 {deviceIndex} → 토픽: {topicPrefix}, 포트: {GetDevicePort(deviceIndex)}");
            
            // 🎯 고정 토픽으로 Fire-and-Forget 전송
            SendFireAndForgetWithTopic(topicPrefix, deviceId, command, data);
        }
        
        /// <summary>
        /// 🔥 특정 토픽으로 Fire-and-Forget 전송
        /// </summary>
        private void SendFireAndForgetWithTopic(string topicPrefix, string deviceId, string command, object data = null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var message = new { deviceId, command, data, timestamp = DateTime.Now, fireAndForget = true };
                    string messageJson = JsonSerializer.Serialize(message);
                    
                    // 🎯 고정 토픽으로 발행 (예: dev_1/command)
                    string publishTopic = $"{topicPrefix}/command";
                    
                    if (true) // Test Mode (시뮬레이션)
                    {
                        await mqttCore.SendMessageAsync(publishTopic, messageJson);
                        // 시뮬레이션 모드에서는 로컬 핸들러도 호출
                        await SimulateLocalEventHandling(deviceId, command, data);
                    }
                    else
                    {
                        await realMqtt.PublishAsync(publishTopic, messageJson);
                    }
                    
                    Console.WriteLine($"🔥 고정 토픽 Fire-and-Forget: {publishTopic}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"🔥 Fire-and-Forget 전송 실패: {ex.Message}");
                }
            });
        }
        
        /// <summary>
        /// 🎯 모든 매핑 정보 출력 (디버깅용)
        /// </summary>
        public void PrintDeviceMappings()
        {
            Console.WriteLine("🎯 고정 디바이스 매핑:");
            for (int i = 0; i < 4; i++)
            {
                var mapping = sharedData.GetSlotMapping(i);
                Console.WriteLine($"  Device {i}: {mapping.PubTopic} → Port {mapping.SubPort}");
            }
        }
        
        #endregion
        
        #region 🔧 MQTT 모드 관리
        
        /// <summary>
        /// 🌐 실제 MQTT 모드로 전환
        /// </summary>
        public async Task<bool> SwitchToRealModeAsync()
        {
            try
            {
                // SimulationMode = false;  // 주석 처리
                bool connected = await realMqtt.ConnectAsync();
                
                if (connected)
                {
                    Console.WriteLine("🌐 실제 MQTT 모드로 전환 완료!");
                    return true;
                }
                else
                {
                    Console.WriteLine("❌ 실제 MQTT 연결 실패 - 시뮬레이션 모드로 복원");
                    // SimulationMode = true;  // 주석 처리
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ MQTT 모드 전환 오류: {ex.Message}");
                // SimulationMode = true;  // 주석 처리
                return false;
            }
        }
        
        /// <summary>
        /// 🎮 시뮬레이션 모드로 전환
        /// </summary>
        public void SwitchToSimulationMode()
        {
            // SimulationMode = true;  // 주석 처리
            realMqtt.Disconnect();
            Console.WriteLine("🎮 시뮬레이션 모드로 전환 완료!");
        }
        
        /// <summary>
        /// 📊 현재 MQTT 모드 확인
        /// </summary>
        public string GetCurrentMode()
        {
            if (true) // 항상 Test Mode
            {
                return "🎮 Test 모드";
            }
            else if (realMqtt.IsConnected)
            {
                return "🌐 실제 MQTT 모드 (연결됨)";
            }
            else
            {
                return "🌐 실제 MQTT 모드 (연결 끊어짐)";
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// MQTT 명령 구조
    /// </summary>
    public class MqttCommand
    {
        public string RequestId { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string Command { get; set; } = "";
        public object Data { get; set; } = null;
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// MQTT 응답 구조 (유연한 필드 지원)
    /// </summary>
    public class MqttResponse
    {
        public string RequestId { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Message { get; set; } = "";
        public object Data { get; set; } = null;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        // 소문자 필드도 지원 (JSON 호환성)
        public string requestId 
        { 
            get => RequestId; 
            set => RequestId = value; 
        }
        
        public string deviceId 
        { 
            get => DeviceId; 
            set => DeviceId = value; 
        }
        
        public string status 
        { 
            get => Status; 
            set => Status = value; 
        }
        
        public string message 
        { 
            get => Message; 
            set => Message = value; 
        }
        
        public object data 
        { 
            get => Data; 
            set => Data = value; 
        }
        
        public DateTime timestamp 
        { 
            get => Timestamp; 
            set => Timestamp = value; 
        }
    }

    public class MqttRes
    {
        [JsonPropertyName("res")]
        public string Res { get; set; } = "";
        [JsonPropertyName("req_by")]
        public string Req_by { get; set; } = "";
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = "";
        [JsonPropertyName("result")]
        public bool Result { get; set; } = true;
        [JsonPropertyName("date")]
        public string Date { get; set; } = "";
        [JsonPropertyName("params")]
        public JsonElement Params { get; set; }    
    }
} 
