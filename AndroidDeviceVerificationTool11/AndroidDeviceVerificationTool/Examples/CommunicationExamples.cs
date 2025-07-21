using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AndroidDeviceVerificationTool.Data;

namespace AndroidDeviceVerificationTool.Examples
{
    /// <summary>
    /// 🎯 PC ↔ Android 4가지 통신 패턴 완전 예제
    /// 데이터 주고받는 사이클을 이해할 수 있도록 실제 동작하는 예제들
    /// 
    /// 📱 Android 개발자를 위한 가이드:
    /// 이 파일의 PC 코드를 참고하여 Android쪽 코드를 만드세요!
    /// 
    /// =================== 📱 ANDROID 코드 예시 ===================
    /// 
    /// // 📦 build.gradle (Module: app)
    /// dependencies {
    ///     implementation 'org.eclipse.paho:org.eclipse.paho.client.mqttv3:1.2.5'
    ///     implementation 'org.eclipse.paho:org.eclipse.paho.android.service:1.1.1'
    /// }
    /// 
    /// // 🎯 AndroidMqttManager.kt
    /// class AndroidMqttManager(private val deviceId: String) {
    ///     private val mqttClient = MqttClient("tcp://192.168.1.100:1883", deviceId, MemoryPersistence())
    ///     private val waitingRequests = ConcurrentHashMap<String, CompletableDeferred<JSONObject>>()
    ///     
    ///     init {
    ///         setupMqttListener()
    ///         mqttClient.connect()
    ///     }
    ///     
    ///     // 📤 패턴1: PC → Android 명령 수신 (SendAndWait 응답)
    ///     private fun setupMqttListener() {
    ///         mqttClient.setCallback(object : MqttCallback {
    ///             override fun messageArrived(topic: String?, message: MqttMessage?) {
    ///                 val payload = String(message!!.payload)
    ///                 val json = JSONObject(payload)
    ///                 
    ///                 when (json.getString("command")) {
    ///                     "test_camera" -> handleCameraTest(json)
    ///                     "brightness_up" -> handleBrightnessUp(json)
    ///                     "volume_up" -> handleVolumeUp(json)
    ///                 }
    ///             }
    ///         })
    ///         mqttClient.subscribe("device/$deviceId/command")
    ///     }
    ///     
    ///     // 📤 패턴1 Android 구현: 카메라 테스트 후 PC에 응답
    ///     private fun handleCameraTest(command: JSONObject) {
    ///         Thread {
    ///             try {
    ///                 // 실제 카메라 테스트 로직
    ///                 val result = performCameraTest()
    ///                 
    ///                 // PC에 응답 전송
    ///                 val response = JSONObject().apply {
    ///                     put("requestId", command.getString("requestId"))
    ///                     put("status", if (result) "PASS" else "FAIL")
    ///                     put("message", "카메라 테스트 완료")
    ///                     put("data", JSONObject().apply {
    ///                         put("camera_id", "front")
    ///                         put("resolution", "1080p")
    ///                         put("test_duration", "2.5s")
    ///                     })
    ///                 }
    ///                 
    ///                 mqttClient.publish("pc/response", MqttMessage(response.toString().toByteArray()))
    ///             } catch (e: Exception) {
    ///                 // 에러 응답
    ///                 val errorResponse = JSONObject().apply {
    ///                     put("requestId", command.getString("requestId"))
    ///                     put("status", "ERROR")
    ///                     put("message", e.message)
    ///                 }
    ///                 mqttClient.publish("pc/response", MqttMessage(errorResponse.toString().toByteArray()))
    ///             }
    ///         }.start()
    ///     }
    ///     
    ///     // 🔥 패턴2 Android 구현: Fire-and-Forget 명령 처리
    ///     private fun handleBrightnessUp(command: JSONObject) {
    ///         val level = command.getJSONObject("data").getString("level")
    ///         // 밝기 조절 (응답 없음)
    ///         adjustBrightness(level)
    ///     }
    ///     
    ///     private fun handleVolumeUp(command: JSONObject) {
    ///         val level = command.getJSONObject("data").getString("level")
    ///         // 볼륨 조절 (응답 없음)  
    ///         adjustVolume(level)
    ///     }
    ///     
    ///     // 📲 패턴3: Android에서 PC로 요청하고 응답 받기
    ///     suspend fun requestConfigFromPC(): JSONObject {
    ///         return sendAndWait("get_config", JSONObject().apply {
    ///             put("app_version", "1.0.0")
    ///             put("reason", "Initial setup")
    ///         })
    ///     }
    ///     
    ///     suspend fun sendAndWait(command: String, data: JSONObject): JSONObject {
    ///         val requestId = UUID.randomUUID().toString()
    ///         val deferred = CompletableDeferred<JSONObject>()
    ///         waitingRequests[requestId] = deferred
    ///         
    ///         val message = JSONObject().apply {
    ///             put("requestId", requestId)
    ///             put("deviceId", deviceId)
    ///             put("command", command)
    ///             put("data", data)
    ///         }
    ///         
    ///         mqttClient.publish("pc/command", MqttMessage(message.toString().toByteArray()))
    ///         return withTimeout(30000) { deferred.await() }
    ///     }
    ///     
    ///     // 📡 패턴4: Android에서 PC로 이벤트 전송 (Fire-and-Forget)
    ///     fun sendBatteryEvent(level: Int) {
    ///         val event = JSONObject().apply {
    ///             put("deviceId", deviceId)
    ///             put("command", "battery_changed")
    ///             put("data", level)
    ///             put("fireAndForget", true)
    ///         }
    ///         mqttClient.publish("pc/command", MqttMessage(event.toString().toByteArray()))
    ///     }
    ///     
    ///     fun sendTestCompletedEvent(testName: String, result: String) {
    ///         val event = JSONObject().apply {
    ///             put("deviceId", deviceId)
    ///             put("command", "test_completed")
    ///             put("data", "$testName: $result")
    ///             put("fireAndForget", true)
    ///         }
    ///         mqttClient.publish("pc/command", MqttMessage(event.toString().toByteArray()))
    ///     }
    ///     
    ///     fun sendErrorEvent(errorMessage: String) {
    ///         val event = JSONObject().apply {
    ///             put("deviceId", deviceId)
    ///             put("command", "error_occurred")
    ///             put("data", errorMessage)
    ///             put("fireAndForget", true)
    ///         }
    ///         mqttClient.publish("pc/command", MqttMessage(event.toString().toByteArray()))
    ///     }
    /// }
    /// 
    /// // 📱 MainActivity.kt 사용 예시
    /// class MainActivity : AppCompatActivity() {
    ///     private lateinit var mqttManager: AndroidMqttManager
    ///     
    ///     override fun onCreate(savedInstanceState: Bundle?) {
    ///         super.onCreate(savedInstanceState)
    ///         
    ///         mqttManager = AndroidMqttManager("ANDROID_${Build.SERIAL}_001")
    ///         
    ///         // 📲 패턴3: PC에서 설정 요청
    ///         lifecycleScope.launch {
    ///             try {
    ///                 val config = mqttManager.requestConfigFromPC()
    ///                 val serverIp = config.getJSONObject("data").getString("server_ip")
    ///                 // 설정 적용...
    ///             } catch (e: Exception) {
    ///                 Log.e("MQTT", "설정 요청 실패", e)
    ///             }
    ///         }
    ///         
    ///         // 📡 패턴4: 배터리 상태 모니터링
    ///         val batteryReceiver = object : BroadcastReceiver() {
    ///             override fun onReceive(context: Context?, intent: Intent?) {
    ///                 val level = intent?.getIntExtra(BatteryManager.EXTRA_LEVEL, -1) ?: -1
    ///                 mqttManager.sendBatteryEvent(level)
    ///             }
    ///         }
    ///         registerReceiver(batteryReceiver, IntentFilter(Intent.ACTION_BATTERY_CHANGED))
    ///     }
    /// }
    /// 
    /// ============= 📱 ANDROID 코드 예시 끝 =============
    /// 
    /// </summary>
    public class CommunicationExamples
    {
        private readonly UnifiedMqttManager mqtt;
        private readonly SharedApplicationData sharedData;
        
    public CommunicationExamples()
        {
            mqtt = UnifiedMqttManager.Instance;
            sharedData = SharedApplicationData.Instance;
        }

        #region 📤 패턴 1: PC → Android + AWAIT (응답 기다리기)
        
        /// <summary>
        /// 📤 패턴 1: PC에서 Android로 명령 보내고 응답 기다리기
        /// 
        /// 🔄 데이터 사이클:
        /// 1. PC: "카메라 테스트 해줘" → MQTT 전송
        /// 2. Android: MQTT 메시지 수신 → 카메라 테스트 실행
        /// 3. Android: 테스트 결과 → MQTT로 PC에 응답
        /// 4. PC: 응답 수신 → await 완료 → 결과 받음
        /// </summary>
        public async Task<string> Example1_PC_To_Android_WithAwait()
        {
            try
            {
                string deviceId = "TEST_DEVICE_001";
                
                sharedData.AddDeviceLog(0, "📤 [패턴1] PC → Android 카메라 테스트 요청 (응답 대기)", "INFO");


                // 🔄 사이클 시작: PC가 Android에게 명령 전송하고 응답 기다림
                var result = await mqtt.SendAndWait(deviceId, "test_camera", new 
                { 
                    timeout = 30000,
                    test_params = "front_camera",
                    quality = "1080p"
                });
                
                sharedData.AddDeviceLog(0, $"📥 [패턴1] Android 응답 수신: {result.Status} - {result.Message}", 
                    result.Status == "PASS" ? "SUCCESS" : "ERROR");
                
                return $"카메라 테스트: {result.Status}";
            }
            catch (TimeoutException)
            {
                sharedData.AddDeviceLog(0, "⏰ [패턴1] 타임아웃: Android가 30초 내에 응답하지 않음", "ERROR");
                return "TIMEOUT";
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴1] 오류: {ex.Message}", "ERROR");
                return "ERROR";
            }
        }

        #endregion

        #region 🔥 패턴 2: PC → Android + FIRE-AND-FORGET (응답 안기다림)
        
        /// <summary>
        /// 🔥 패턴 2: PC에서 Android로 명령 보내고 응답 안기다리기
        /// 
        /// 🔄 데이터 사이클:
        /// 1. PC: "화면 밝기 올려줘" → MQTT 전송 ⚡즉시 완료!
        /// 2. PC: 즉시 완료 (응답 기다리지 않음)
        /// 3. Android: MQTT 메시지 수신 → 밝기 조절 실행
        /// 4. Android: 완료 (PC에 알리지 않음)
        /// </summary>
        public async Task Example2_PC_To_Android_FireAndForget()
        {
            try
            {
                string deviceId = "TEST_DEVICE_001";
                
                sharedData.AddDeviceLog(0, "🔥 [패턴2] PC → Android 밝기 증가 명령 (Fire-and-Forget)", "INFO");
                
                // 🔥 진짜 Fire-and-Forget: await 없이 즉시 완료!
                mqtt.SendFireAndForget(deviceId, "brightness_up", new 
                { 
                    level = "+20",
                    smooth_transition = true
                });
                
                sharedData.AddDeviceLog(0, "⚡ [패턴2] 명령 전송 즉시 완료! (Fire-and-Forget)", "SUCCESS");
                
                // 추가 명령들도 연속으로 전송 가능
                await Task.Delay(500); // UI 시각화용 딜레이
                mqtt.SendFireAndForget(deviceId, "volume_up", new { level = "+10" });
                sharedData.AddDeviceLog(0, "🔥 [패턴2] 볼륨 증가 명령도 즉시 전송!", "INFO");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴2] 오류: {ex.Message}", "ERROR");
            }
        }

        #endregion

        #region 📲 패턴 3: Android → PC + AWAIT (Kotlin에서 PC로 요청)
        
        /// <summary>
        /// 📲 패턴 3: Android에서 PC로 요청하고 응답 기다리기 - 핸들러 등록
        /// 
        /// 🔄 데이터 사이클:
        /// 1. Android: "설정 정보 보내줘" → MQTT로 PC에 요청  
        /// 2. PC: MQTT 메시지 수신 → 설정 정보 준비
        /// 3. PC: 설정 정보 → MQTT로 Android에 응답
        /// 4. Android: 응답 수신 → await 완료 → 설정 적용
        /// 
        /// ⚠️ 실제 Android 기기가 연결되어야 테스트 가능합니다!
        /// 이 메서드는 PC쪽 핸들러만 등록합니다.
        /// </summary>
        public async Task Example3_RegisterHandler_Only()
        {
            try
            {
                sharedData.AddDeviceLog(0, "🚀 [패턴3] 타이머 시뮬레이션 테스트 시작!", "INFO");
                
                // PC쪽 핸들러 등록
                SetupPattern3_AndroidToPC_Handler();
                
                await Task.Delay(500); // 핸들러 등록 완료 대기
                
                sharedData.AddDeviceLog(0, "✅ [패턴3] PC 핸들러 등록 완료!", "SUCCESS");
                sharedData.AddDeviceLog(0, "⏰ [패턴3] 3초 후 타이머로 Android 시뮬레이션 시작...", "INFO");
                
                // 🎯 타이머로 Android 역할 시뮬레이션
                _ = Task.Run(async () =>
                {
                    await Task.Delay(3000); // 3초 대기
                    
                    sharedData.AddDeviceLog(0, "🤖 [패턴3] 타이머 발동! Android 역할로 get_config 요청 전송!", "INFO");
                    
                    var response = await SimulateAndroidRequestToPC();
                    
                    if (response != null)
                    {
                        sharedData.AddDeviceLog(0, $"✅ [패턴3] 🎉 성공! 받은 설정 - IP: {((dynamic)response.Data)?.server_ip}", "SUCCESS");
                        sharedData.AddDeviceLog(0, $"📋 [패턴3] 설정 포트: {((dynamic)response.Data)?.server_port}, API키: {((dynamic)response.Data)?.api_key}", "SUCCESS");
                        sharedData.AddDeviceLog(0, "🎊 [패턴3] 완전한 Android→PC 사이클 완료! (타이머 시뮬레이션)", "SUCCESS");
                    }
                    else
                    {
                        sharedData.AddDeviceLog(0, "❌ [패턴3] 응답 수신 실패 (타임아웃?)", "ERROR");
                    }
                });
                
                sharedData.AddDeviceLog(0, "⏳ [패턴3] 타이머 시뮬레이션 대기중... (실제 기기 없이도 완전한 사이클 체험!)", "SUCCESS");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴3] 타이머 시뮬레이션 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 🎯 PC쪽 핸들러만 등록 (기존 메서드)
        /// </summary>
        public void SetupPattern3_AndroidToPC_Handler()
        {
            // 🎯 Android → PC 요청 핸들러 등록
            mqtt.RegisterHandler("get_config", async (command) =>
            {
                sharedData.AddDeviceLog(0, $"📥 [패턴3] PC 핸들러 실행! 요청자: {command.DeviceId}", "INFO");
                
                // PC에서 설정 정보 준비 (실제 로직)
                var configData = new
                {
                    server_ip = "192.168.1.100",
                    server_port = 8080,
                    api_key = "ABC123XYZ",
                    test_mode = true,
                    max_retry = 3,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                sharedData.AddDeviceLog(0, "📤 [패턴3] PC에서 Android로 설정 정보 응답 전송", "SUCCESS");
                
                return new MqttResponse 
                { 
                    Status = "OK", 
                    Message = "설정 정보 전송 완료",
                    Data = configData
                };
            });
            
            sharedData.AddDeviceLog(0, "🎯 [패턴3] Android → PC 핸들러 등록 완료 (get_config)", "INFO");
        }
        
        /// <summary>
        /// 🤖 Android 역할 시뮬레이션: PC에 설정 요청 (시뮬레이션 모드)
        /// </summary>
        private async Task<MqttResponse?> SimulateAndroidRequestToPC()
        {
            try
            {
                string deviceId = "ANDROID_SIMULATOR_001";
                
                // 🎮 시뮬레이션: 직접 핸들러 호출 (MQTT 없이)
                var command = new MqttCommand
                {
                    RequestId = Guid.NewGuid().ToString(),
                    DeviceId = deviceId,
                    Command = "get_config",
                    Data = new 
                    { 
                        requested_by = "Android App",
                        app_version = "1.0.0",
                        reason = "Initial setup"
                    },
                    Timestamp = DateTime.Now
                };
                
                // 등록된 get_config 핸들러 직접 호출
                var configHandler = typeof(UnifiedMqttManager)
                    .GetField("eventHandlers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(mqtt) as Dictionary<string, Func<MqttCommand, Task<MqttResponse>>>;
                
                if (configHandler?.TryGetValue("get_config", out var handler) == true)
                {
                    return await handler(command);
                }
                
                // 핸들러가 없으면 기본 응답
                return new MqttResponse 
                { 
                    Status = "ERROR", 
                    Message = "get_config 핸들러를 찾을 수 없습니다",
                    RequestId = command.RequestId
                };
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴3] Android 시뮬레이션 오류: {ex.Message}", "ERROR");
                return null;
            }
        }

        #endregion

        #region 📡 패턴 4: Android → PC + EVENT (Android에서 PC로 이벤트 알림)
        
        /// <summary>
        /// 📡 패턴 4: Android에서 PC로 이벤트 알림 - 완전한 사이클 테스트
        /// 
        /// 🔄 데이터 사이클:
        /// 1. Android: "배터리 20%입니다" → MQTT로 PC에 이벤트 전송
        /// 2. Android: 즉시 완료 (응답 기다리지 않음)
        /// 3. PC: MQTT 이벤트 수신 → 이벤트 처리 (UI 업데이트 등)
        /// 4. PC: 처리 완료 (Android에 알리지 않음)
        /// 
        /// 🚀 이 메서드는 리스너 등록 + 즉시 이벤트 발생 시뮬레이션까지 합니다!
        /// </summary>
        public async Task Example4_Complete_AndroidToPC_Events()
        {
            try
            {
                sharedData.AddDeviceLog(0, "🚀 [패턴4] 완전한 이벤트 사이클 테스트 시작!", "INFO");
                
                // 1단계: PC쪽 이벤트 리스너 등록
                SetupPattern4_AndroidToPC_EventListener();
                
                await Task.Delay(500); // 리스너 등록 완료 대기
                
                // 2단계: Android 역할로 여러 이벤트 발생 시뮬레이션
                sharedData.AddDeviceLog(0, "📱 [패턴4] Android 역할: 배터리 이벤트 발생...", "INFO");
                await SimulateAndroidBatteryEvent(75);
                
                await Task.Delay(800);
                sharedData.AddDeviceLog(0, "📱 [패턴4] Android 역할: 테스트 완료 이벤트 발생...", "INFO");
                await SimulateAndroidTestCompletedEvent("카메라테스트", "PASS");
                
                await Task.Delay(800);
                sharedData.AddDeviceLog(0, "📱 [패턴4] Android 역할: 배터리 부족 이벤트 발생...", "INFO");
                await SimulateAndroidBatteryEvent(15);
                
                await Task.Delay(800);
                sharedData.AddDeviceLog(0, "📱 [패턴4] Android 역할: 에러 이벤트 발생...", "INFO");
                await SimulateAndroidErrorEvent("테스트 시뮬레이션 에러");
                
                sharedData.AddDeviceLog(0, "🎉 [패턴4] 완전한 Android→PC 이벤트 사이클 완료!", "SUCCESS");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴4] 이벤트 사이클 테스트 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 🎯 PC쪽 이벤트 리스너만 등록 (기존 메서드)
        /// </summary>
        public void SetupPattern4_AndroidToPC_EventListener()
        {
            // 🎯 Android → PC 이벤트 리스너 등록
            mqtt.RegisterHandler("battery_changed", (command) =>
            {
                var batteryLevel = command.Data?.ToString();
                sharedData.AddDeviceLog(0, $"📡 [패턴4] Android 배터리 이벤트: {batteryLevel}%", "INFO");
                
                // 배터리 레벨에 따른 처리
                if (int.TryParse(batteryLevel, out int level))
                {
                    if (level < 20)
                    {
                        sharedData.AddDeviceLog(0, "⚠️ [패턴4] 배터리 부족 경고!", "WARNING");
                    }
                    else if (level > 80)
                    {
                        sharedData.AddDeviceLog(0, "🔋 [패턴4] 배터리 충분함", "SUCCESS");
                    }
                }
                
                return new MqttResponse { Status = "OK", Message = "배터리 이벤트 처리 완료" };
            });
            
            mqtt.RegisterHandler("test_completed", (command) =>
            {
                var testName = command.Data?.ToString();
                sharedData.AddDeviceLog(0, $"📡 [패턴4] Android 테스트 완료 이벤트: {testName}", "SUCCESS");
                return new MqttResponse { Status = "OK", Message = "테스트 완료 이벤트 처리" };
            });
            
            mqtt.RegisterHandler("error_occurred", (command) =>
            {
                var errorMsg = command.Data?.ToString();
                sharedData.AddDeviceLog(0, $"📡 [패턴4] Android 에러 이벤트: {errorMsg}", "ERROR");
                return new MqttResponse { Status = "OK", Message = "에러 이벤트 처리 완료" };
            });
            
            sharedData.AddDeviceLog(0, "🎯 [패턴4] Android → PC 이벤트 리스너 등록 완료", "INFO");
        }
        
        /// <summary>
        /// 🤖 Android 역할 시뮬레이션: 배터리 이벤트 발생
        /// </summary>
        private async Task SimulateAndroidBatteryEvent(int batteryLevel)
        {
            try
            {
                string deviceId = "ANDROID_SIMULATOR_001";
                
                // Android가 PC로 배터리 이벤트 전송 (Fire-and-Forget)
                mqtt.SendFireAndForget(deviceId, "battery_changed", batteryLevel);
                
                await Task.Delay(100); // 이벤트 처리 대기
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴4] 배터리 이벤트 시뮬레이션 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 🤖 Android 역할 시뮬레이션: 테스트 완료 이벤트 발생
        /// </summary>
        private async Task SimulateAndroidTestCompletedEvent(string testName, string result)
        {
            try
            {
                string deviceId = "ANDROID_SIMULATOR_001";
                
                // Android가 PC로 테스트 완료 이벤트 전송 (Fire-and-Forget)
                mqtt.SendFireAndForget(deviceId, "test_completed", $"{testName}: {result}");
                
                await Task.Delay(100); // 이벤트 처리 대기
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴4] 테스트 완료 이벤트 시뮬레이션 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 🤖 Android 역할 시뮬레이션: 에러 이벤트 발생
        /// </summary>
        private async Task SimulateAndroidErrorEvent(string errorMessage)
        {
            try
            {
                string deviceId = "ANDROID_SIMULATOR_001";
                
                // Android가 PC로 에러 이벤트 전송 (Fire-and-Forget)
                mqtt.SendFireAndForget(deviceId, "error_occurred", errorMessage);
                
                await Task.Delay(100); // 이벤트 처리 대기
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ [패턴4] 에러 이벤트 시뮬레이션 오류: {ex.Message}", "ERROR");
            }
        }

        #endregion

        #region 🧪 통합 테스트 메서드들

        /// <summary>
        /// 🚀 모든 패턴을 순서대로 테스트하는 통합 메서드
        /// </summary>
        public async Task TestAllPatterns()
        {
            sharedData.AddDeviceLog(0, "🚀 4가지 통신 패턴 전체 테스트 시작!", "INFO");
            
            try
            {
                // 패턴 3, 4 핸들러 먼저 등록
                SetupPattern3_AndroidToPC_Handler();
                SetupPattern4_AndroidToPC_EventListener();
                
                await Task.Delay(500);
                
                // 패턴 1: PC → Android (응답 기다림)
                sharedData.AddDeviceLog(0, "📋 1단계: PC → Android (AWAIT) 테스트", "INFO");
                string result1 = await Example1_PC_To_Android_WithAwait();
                await Task.Delay(1000);
                
                // 패턴 2: PC → Android (Fire-and-Forget)
                sharedData.AddDeviceLog(0, "📋 2단계: PC → Android (Fire-and-Forget) 테스트", "INFO");
                await Example2_PC_To_Android_FireAndForget();
                await Task.Delay(1000);
                
                // 패턴 3, 4는 Android에서 시작하므로 시뮬레이션
                sharedData.AddDeviceLog(0, "📋 3단계: Android → PC 패턴들은 Android에서 시작됩니다", "INFO");
                sharedData.AddDeviceLog(0, "📲 Android에서 get_config 요청하면 패턴3 동작", "INFO");
                sharedData.AddDeviceLog(0, "📡 Android에서 이벤트 전송하면 패턴4 동작", "INFO");
                
                sharedData.AddDeviceLog(0, "🎉 4가지 통신 패턴 테스트 완료!", "SUCCESS");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ 통합 테스트 오류: {ex.Message}", "ERROR");
            }
        }

        /// <summary>
        /// 🎯 특정 패턴만 테스트
        /// </summary>
        public async Task TestSpecificPattern(int patternNumber)
        {
            switch (patternNumber)
            {
                case 1:
                    await Example1_PC_To_Android_WithAwait();
                    break;
                case 2:
                    await Example2_PC_To_Android_FireAndForget();
                    break;
                case 3:
                    SetupPattern3_AndroidToPC_Handler();
                    sharedData.AddDeviceLog(0, "📲 패턴3 핸들러 등록됨. Android에서 요청 대기중...", "INFO");
                    break;
                case 4:
                    SetupPattern4_AndroidToPC_EventListener();
                    sharedData.AddDeviceLog(0, "📡 패턴4 리스너 등록됨. Android 이벤트 대기중...", "INFO");
                    break;
                default:
                    sharedData.AddDeviceLog(0, "❌ 잘못된 패턴 번호 (1-4만 지원)", "ERROR");
                    break;
            }
        }

        #endregion
    }
} 