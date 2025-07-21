using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// 안드로이드 디바이스 검증 도구의 통합 API 클래스
    /// </summary>
    public class AndroidDeviceAPI
    {
        #region 델리게이트 정의 (콜백 함수용)
        
        /// <summary>
        /// 디바이스 관련 콜백 함수 델리게이트
        /// </summary>
        /// <param name="parameters">가변 파라미터</param>
        public delegate void DeviceCallback(params object[] parameters);
        
        /// <summary>
        /// 데이터베이스 관련 콜백 함수 델리게이트
        /// </summary>
        /// <param name="parameters">가변 파라미터</param>
        public delegate void DatabaseCallback(params object[] parameters);
        
        /// <summary>
        /// 메시지 수신 콜백 함수 델리게이트
        /// </summary>
        /// <param name="msgName">메시지 이름</param>
        /// <param name="parameters">가변 파라미터</param>
        public delegate void MessageReceiveCallback(string msgName, params object[] parameters);
        
        #endregion

        #region 이벤트
        
        /// <summary>
        /// 디바이스 이벤트
        /// </summary>
        public event DeviceCallback OnDeviceEvent;
        
        /// <summary>
        /// 데이터베이스 이벤트 (연결상태: connected, disconnected)
        /// </summary>
        public event DatabaseCallback OnDatabaseEvent;
        
        /// <summary>
        /// 메시지 수신 이벤트
        /// </summary>
        public event MessageReceiveCallback OnMessageReceived;
        
        #endregion

        #region 싱글톤 패턴 및 의존성 주입
        
        private static AndroidDeviceAPI _instance;
        private static readonly object _lock = new object();
        
        // 기존 프로젝트 클래스들 참조
        private readonly AdbDeviceMonitor adbMonitor;
        private readonly DeviceManager deviceManager;
        private readonly UnifiedMqttManager mqttManager;
        private readonly ConfigManager configManager;
        private readonly SharedApplicationData sharedData;
        private readonly Dictionary<string, string> deviceTopics = new();
        
        public static AndroidDeviceAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new AndroidDeviceAPI();
                    }
                }
                return _instance;
            }
        }
        
        private AndroidDeviceAPI() 
        {
            // 기존 프로젝트 클래스들 초기화
            adbMonitor = new AdbDeviceMonitor();
            deviceManager = DeviceManager.Instance;
            mqttManager = UnifiedMqttManager.Instance;
            configManager = ConfigManager.Instance;
            sharedData = SharedApplicationData.Instance;
            
            // 이벤트 연결
            SetupEventHandlers();
            
            // 기본 디바이스 설정
            sharedData.SetupBasicDevices();
        }
        
        /// <summary>
        /// 이벤트 핸들러 설정
        /// </summary>
        private void SetupEventHandlers()
        {
            // ADB 디바이스 연결/해제 이벤트
            adbMonitor.DeviceConnected += OnAdbDeviceConnected;
            adbMonitor.DeviceDisconnected += OnAdbDeviceDisconnected;
            adbMonitor.LogMessage += (msg) => Console.WriteLine($"[ADB] {msg}");
            
            // 디바이스 매니저 이벤트
            deviceManager.DeviceConnectionChanged += OnDeviceConnectionChanged;
            
            // 공유 데이터 이벤트
            sharedData.DeviceConnectionChanged += OnSharedDeviceConnectionChanged;
        }
        
        #endregion

        #region 이벤트 핸들러들
        
        /// <summary>
        /// ADB 디바이스 연결 시 호출
        /// </summary>
        private void OnAdbDeviceConnected(AdbDevice device)
        {
            try
            {
                Console.WriteLine($"[API] ADB Device Connected: {device.DeviceId}");
                
                // 사용 가능한 슬롯 찾기
                var availableSlot = FindAvailableSlot();
                if (availableSlot >= 0)
                {
                    // 슬롯에 디바이스 할당
                    sharedData.AssignDeviceToSlot(device.DeviceId, availableSlot);
                    deviceTopics[device.DeviceId] = $"dev_{availableSlot + 1}";
                    
                    // 콜백 이벤트 발생
                    OnDeviceEvent?.Invoke("connected", device.DeviceId, availableSlot);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error handling ADB device connection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// ADB 디바이스 연결 해제 시 호출
        /// </summary>
        private void OnAdbDeviceDisconnected(string deviceId)
        {
            try
            {
                Console.WriteLine($"[API] ADB Device Disconnected: {deviceId}");
                
                // 해당 디바이스가 할당된 슬롯 찾기
                var slot = FindSlotByDeviceId(deviceId);
                if (slot >= 0)
                {
                    sharedData.DisconnectDevice(slot);
                    deviceTopics.Remove(deviceId);
                    
                    // 콜백 이벤트 발생
                    OnDeviceEvent?.Invoke("disconnected", deviceId, slot);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error handling ADB device disconnection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 디바이스 매니저 연결 상태 변경 시 호출
        /// </summary>
        private void OnDeviceConnectionChanged(int index, SharedDeviceData device)
        {
            try
            {
                Console.WriteLine($"[API] Device Manager Connection Changed: {index} - {device.DeviceId}");
                OnDeviceEvent?.Invoke("connection_changed", device.DeviceId, index, device.IsConnected);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error handling device manager connection change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 공유 데이터 연결 상태 변경 시 호출
        /// </summary>
        private void OnSharedDeviceConnectionChanged(int index, UnifiedDeviceData device)
        {
            try
            {
                Console.WriteLine($"[API] Shared Data Connection Changed: {index} - {device.DeviceId}");
                OnDeviceEvent?.Invoke("shared_connection_changed", device.DeviceId, index, device.IsConnected);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error handling shared data connection change: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 사용 가능한 슬롯 찾기 (0-3)
        /// </summary>
        private int FindAvailableSlot()
        {
            for (int i = 0; i < 4; i++)
            {
                var device = sharedData.GetDeviceByIndex(i);
                if (device != null && !device.IsConnected)
                {
                    return i;
                }
            }
            return -1; // 사용 가능한 슬롯 없음
        }
        
        /// <summary>
        /// 디바이스 ID로 슬롯 찾기
        /// </summary>
        private int FindSlotByDeviceId(string deviceId)
        {
            for (int i = 0; i < 4; i++)
            {
                var device = sharedData.GetDeviceByIndex(i);
                if (device != null && device.DeviceId == deviceId)
                {
                    return i;
                }
            }
            return -1;
        }
        
        #endregion

        #region 앱 관리 함수
        
        /// <summary>
        /// APK 파일 설치 (api.txt: install_app)
        /// </summary>
        /// <param name="apk_filepath">APK 파일 경로</param>
        /// <returns>설치 성공 여부</returns>
        public async Task<bool> install_app(string apk_filepath)
        {
            try
            {
                if (!File.Exists(apk_filepath))
                {
                    Console.WriteLine($"APK file not found: {apk_filepath}");
                    return false;
                }
                
                // 연결된 디바이스 확인
                var connectedDevices = await adbMonitor.GetConnectedDevicesAsync();
                if (connectedDevices.Count == 0)
                {
                    Console.WriteLine("No connected devices found for APK installation");
                    return false;
                }
                
                // 첫 번째 연결된 디바이스에 설치
                var targetDevice = connectedDevices[0];
                Console.WriteLine($"Installing APK on device: {targetDevice.DeviceId}");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {targetDevice.DeviceId} install \"{apk_filepath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    Console.WriteLine("Failed to start ADB process for installation");
                    return false;
                }
                
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0 && output.Contains("Success"))
                {
                    Console.WriteLine($"APK installation successful: {apk_filepath}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"APK installation failed: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to install APK: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// APK 파일 제거 (api.txt: remove_app)
        /// </summary>
        /// <param name="apk_filepath">패키지명 또는 APK 파일 경로</param>
        /// <returns>제거 성공 여부</returns>
        public async Task<bool> remove_app(string apk_filepath)
        {
            try
            {
                // APK 파일 경로인 경우 패키지명 추출
                var packageName = apk_filepath;
                if (packageName.EndsWith(".apk"))
                {
                    packageName = await GetPackageNameFromApk(packageName);
                    if (string.IsNullOrEmpty(packageName))
                    {
                        Console.WriteLine("Failed to extract package name from APK");
                        return false;
                    }
                }
                
                // 연결된 디바이스 확인
                var connectedDevices = await adbMonitor.GetConnectedDevicesAsync();
                if (connectedDevices.Count == 0)
                {
                    Console.WriteLine("No connected devices found for APK removal");
                    return false;
                }
                
                // 첫 번째 연결된 디바이스에서 제거
                var targetDevice = connectedDevices[0];
                Console.WriteLine($"Removing package on device: {targetDevice.DeviceId}");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {targetDevice.DeviceId} uninstall {packageName}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    Console.WriteLine("Failed to start ADB process for removal");
                    return false;
                }
                
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0 && output.Contains("Success"))
                {
                    Console.WriteLine($"Package removal successful: {packageName}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"Package removal failed: {error}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove package: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// APK 파일에서 패키지명 추출
        /// </summary>
        private async Task<string> GetPackageNameFromApk(string apkFilepath)
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "aapt",
                    Arguments = $"dump badging \"{apkFilepath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                if (process == null) return "";
                
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                // package: name='com.example.app' 형태에서 패키지명 추출
                var lines = output.Split('\n');
                foreach (var line in lines)
                {
                    if (line.StartsWith("package: name="))
                    {
                        var start = line.IndexOf("'") + 1;
                        var end = line.IndexOf("'", start);
                        if (start > 0 && end > start)
                        {
                            return line.Substring(start, end - start);
                        }
                    }
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }
        
        #endregion

        #region 디바이스 감지 함수
        
        /// <summary>
        /// 디바이스 감지 시작 (api.txt: start_dev_detect)
        /// </summary>
        /// <returns>수신 토픽</returns>
        public async Task<string> start_dev_detect()
        {
            try
            {
                // ADB 디바이스 모니터링 시작
                await adbMonitor.StartMonitoringAsync();
                
                string recvTopic = $"device_detect_{DateTime.Now.Ticks}";
                Console.WriteLine($"[API] Started ADB device detection with topic: {recvTopic}");
                
                // 초기 연결된 디바이스들 확인
                var initialDevices = await adbMonitor.GetConnectedDevicesAsync();
                Console.WriteLine($"[API] Found {initialDevices.Count} initially connected devices");
                
                foreach (var device in initialDevices)
                {
                    OnAdbDeviceConnected(device);
                }
                
                return recvTopic;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to start device detection: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 디바이스 감지 중지 (api.txt: stop_dev_detect)
        /// </summary>
        /// <param name="recv_topic">수신 토픽</param>
        public async Task stop_dev_detect(string recv_topic)
        {
            try
            {
                // ADB 디바이스 모니터링 중지
                await adbMonitor.StopMonitoringAsync();
                Console.WriteLine($"[API] Stopped device detection for topic: {recv_topic}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to stop device detection: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 디바이스 이벤트 콜백 등록 (api.txt: cb_dev_evt)
        /// </summary>
        /// <param name="callback">콜백 함수</param>
        public void cb_dev_evt(DeviceCallback callback)
        {
            OnDeviceEvent += callback;
        }
        
        #endregion

        #region ADB 포트 포워딩
        
        /// <summary>
        /// ADB 포트 포워딩 열기 (api.txt: open_adb_port_forwarding)
        /// </summary>
        /// <param name="port">포트 번호</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> open_adb_port_forwarding(int port)
        {
            try
            {
                // 연결된 디바이스 확인
                var connectedDevices = await adbMonitor.GetConnectedDevicesAsync();
                if (connectedDevices.Count == 0)
                {
                    Console.WriteLine("[API] No connected devices found for port forwarding");
                    return false;
                }
                
                bool allSuccess = true;
                
                // 모든 연결된 디바이스에 포트 포워딩 설정
                foreach (var device in connectedDevices)
                {
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "adb",
                        Arguments = $"-s {device.DeviceId} forward tcp:{port} tcp:{port}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = Process.Start(processInfo);
                    if (process == null)
                    {
                        Console.WriteLine($"[API] Failed to start ADB process for port forwarding on {device.DeviceId}");
                        allSuccess = false;
                        continue;
                    }
                    
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine($"[API] Port forwarding successful on {device.DeviceId}: {port}");
                    }
                    else
                    {
                        Console.WriteLine($"[API] Port forwarding failed on {device.DeviceId}: {error}");
                        allSuccess = false;
                    }
                }
                
                return allSuccess;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to open ADB port forwarding: {ex.Message}");
                return false;
            }
        }
        
        #endregion

        #region MQTT 관련 함수
        
        /// <summary>
        /// MQTT 수신 채널 열기 (api.txt: mqtt_recv_open)
        /// </summary>
        /// <param name="recv_topic">수신 토픽</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> mqtt_recv_open(string recv_topic)
        {
            try
            {
                // UnifiedMqttManager를 통해 실제 MQTT 연결
                var success = await mqttManager.SwitchToRealModeAsync();
                if (success)
                {
                    Console.WriteLine($"[API] MQTT receive channel opened for topic: {recv_topic}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[API] Failed to switch to real MQTT mode for topic: {recv_topic}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to open MQTT receive channel: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// MQTT 수신 채널 닫기 (api.txt: mqtt_recv_close)
        /// </summary>
        /// <param name="recv_topic">수신 토픽</param>
        /// <returns>성공 여부</returns>
        public bool mqtt_recv_close(string recv_topic)
        {
            try
            {
                // 시뮬레이션 모드로 전환
                mqttManager.SwitchToSimulationMode();
                Console.WriteLine($"[API] MQTT receive channel closed for topic: {recv_topic}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to close MQTT receive channel: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// MQTT 조인 (api.txt: mqtt_join)
        /// </summary>
        /// <param name="dev_sub_topic">디바이스 구독 토픽</param>
        /// <param name="dev_pub_topic">디바이스 발행 토픽</param>
        /// <returns>성공 여부</returns>
        public async Task<bool> mqtt_join(string dev_sub_topic, string dev_pub_topic)
        {
            try
            {
                // MQTT 연결 확인 및 실제 모드 활성화
                var success = await mqttManager.SwitchToRealModeAsync();
                if (success)
                {
                    Console.WriteLine($"[API] MQTT join successful - Sub: {dev_sub_topic}, Pub: {dev_pub_topic}");
                    
                    // 토픽 매핑 저장
                    if (!string.IsNullOrEmpty(dev_pub_topic))
                    {
                        deviceTopics[dev_pub_topic] = dev_sub_topic;
                    }
                    
                    return true;
                }
                else
                {
                    Console.WriteLine($"[API] MQTT join failed - Sub: {dev_sub_topic}, Pub: {dev_pub_topic}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to join MQTT: {ex.Message}");
                return false;
            }
        }
        
        #endregion

        #region 설정 관련 함수
        
        /// <summary>
        /// 설정 정보 가져오기 (api.txt: get_config)
        /// </summary>
        /// <returns>설정 정보 JSON 문자열</returns>
        public string get_config()
        {
            try
            {
                var config = configManager.GetConfig();
                
                // 설정을 간단한 JSON 형태로 변환
                var configInfo = new
                {
                    environment = config.Environment?.Mode ?? "Development",
                    mqtt = new
                    {
                        brokerAddress = config.MQTT?.BrokerAddress ?? "127.0.0.1",
                        port = config.MQTT?.Port ?? 1883,
                        username = config.MQTT?.Username ?? "",
                        enableSSL = config.MQTT?.EnableSSL ?? false
                    },
                    adb = new
                    {
                        monitoringEnabled = config.ADB?.MonitoringEnabled ?? true,
                        intervalSeconds = config.ADB?.MonitoringIntervalSeconds ?? 2,
                        timeoutSeconds = config.ADB?.CommandTimeoutSeconds ?? 10
                    },
                    ui = new
                    {
                        theme = config.UI?.Theme ?? "Dark",
                        refreshInterval = config.UI?.AutoRefreshInterval ?? 1000,
                        maxLogLines = config.UI?.LogMaxLines ?? 1000
                    }
                };
                
                var jsonString = System.Text.Json.JsonSerializer.Serialize(configInfo, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                Console.WriteLine("[API] Configuration retrieved successfully");
                return jsonString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to get configuration: {ex.Message}");
                return "{}";
            }
        }
        
        /// <summary>
        /// 설정 정보 설정 (api.txt: set_config)
        /// </summary>
        /// <param name="db_info">데이터베이스 정보</param>
        /// <param name="wifi_ssid">WiFi SSID</param>
        /// <param name="ble_name">BLE 이름</param>
        public async Task set_config(string db_info, string wifi_ssid, string ble_name)
        {
            try
            {
                // ConfigManager를 통해 설정 업데이트
                await configManager.UpdateConfigAsync(config =>
                {
                    // 필요한 경우 설정 값들 업데이트
                    // 예: config.MQTT.BrokerAddress = db_info;
                    Console.WriteLine($"[API] Setting config - DB: {db_info}, WiFi: {wifi_ssid}, BLE: {ble_name}");
                });
                
                Console.WriteLine("[API] Configuration updated successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to set configuration: {ex.Message}");
            }
        }
        
        #endregion

        #region 데이터베이스 관련 함수
        
        /// <summary>
        /// 데이터베이스 연결 열기
        /// </summary>
        /// <param name="ip">IP 주소</param>
        /// <param name="port">포트 번호</param>
        /// <param name="password">비밀번호</param>
        /// <returns>연결 성공 여부</returns>
        public bool OpenDatabaseConnection(string ip, int port, string password)
        {
            try
            {
                // TODO: 실제 데이터베이스 연결 로직 구현
                Console.WriteLine($"Opening database connection - {ip}:{port}");
                OnDatabaseEvent?.Invoke("connected", ip, port);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to open database connection: {ex.Message}");
                OnDatabaseEvent?.Invoke("disconnected", ip, port);
                return false;
            }
        }
        
        /// <summary>
        /// 데이터베이스 연결 닫기
        /// </summary>
        /// <param name="ip">IP 주소</param>
        /// <param name="port">포트 번호</param>
        /// <param name="password">비밀번호</param>
        /// <returns>연결 종료 성공 여부</returns>
        public bool CloseDatabaseConnection(string ip, int port, string password)
        {
            try
            {
                // TODO: 실제 데이터베이스 연결 종료 로직 구현
                Console.WriteLine($"Closing database connection - {ip}:{port}");
                OnDatabaseEvent?.Invoke("disconnected", ip, port);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to close database connection: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 데이터베이스 이벤트 콜백 등록 (api.txt: cb_db_evt)
        /// </summary>
        /// <param name="callback">콜백 함수</param>
        public void cb_db_evt(DatabaseCallback callback)
        {
            OnDatabaseEvent += callback;
        }
        
        #endregion

        #region 메시지 관련 함수
        
        /// <summary>
        /// 메시지 전송 (api.txt: send_msg)
        /// </summary>
        /// <param name="msg_name">메시지 이름</param>
        /// <param name="parameters">가변 파라미터</param>
        /// <returns>전송 성공 여부</returns>
        public bool send_msg(string msg_name, params object[] parameters)
        {
            try
            {
                // UnifiedMqttManager를 통해 Fire-and-Forget 메시지 전송
                var data = new
                {
                    message = msg_name,
                    parameters = parameters,
                    timestamp = DateTime.Now
                };
                
                // 사용 가능한 디바이스에 메시지 전송
                for (int i = 0; i < 4; i++)
                {
                    var device = sharedData.GetDeviceByIndex(i);
                    if (device != null && device.IsConnected)
                    {
                        mqttManager.SendToDeviceFireAndForget(i, msg_name, data);
                        Console.WriteLine($"[API] Message sent to device {i}: {msg_name}");
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to send message: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 메시지 수신 콜백 등록 (api.txt: cb_recv_msg)
        /// </summary>
        /// <param name="callback">콜백 함수</param>
        public void cb_recv_msg(MessageReceiveCallback callback)
        {
            OnMessageReceived += callback;
        }
        
        /// <summary>
        /// 메시지 수신 처리 (내부적으로 호출됨)
        /// </summary>
        /// <param name="msgName">메시지 이름</param>
        /// <param name="parameters">가변 파라미터</param>
        private void HandleReceivedMessage(string msgName, params object[] parameters)
        {
            OnMessageReceived?.Invoke(msgName, parameters);
        }
        
        #endregion

        #region 테스트 관련 함수
        
        /// <summary>
        /// 자동 테스트 실행 (api.txt: do_auto_test)
        /// </summary>
        /// <param name="test_id">테스트 ID</param>
        /// <param name="parameters">가변 파라미터</param>
        /// <returns>테스트 성공 여부</returns>
        public async Task<bool> do_auto_test(int test_id, params object[] parameters)
        {
            try
            {
                Console.WriteLine($"[API] Running auto test ID: {test_id} with {parameters.Length} parameters");
                
                // 연결된 디바이스 찾기
                var targetSlot = -1;
                for (int i = 0; i < 4; i++)
                {
                    var device = sharedData.GetDeviceByIndex(i);
                    if (device != null && device.IsConnected)
                    {
                        targetSlot = i;
                        break;
                    }
                }
                
                if (targetSlot < 0)
                {
                    Console.WriteLine("[API] No connected devices found for auto test");
                    return false;
                }
                
                // UnifiedMqttManager를 통해 테스트 명령 전송
                var testData = new
                {
                    testType = "auto",
                    testId = test_id,
                    parameters = parameters,
                    timestamp = DateTime.Now
                };
                
                var response = await mqttManager.SendToDevice(targetSlot, "run_auto_test", testData);
                
                if (response.Status == "OK" || response.Status == "PASS")
                {
                    // 테스트 상태 업데이트
                    if (test_id < sharedData.GetDeviceByIndex(targetSlot)?.PeripheralData.AutoTests.Count)
                    {
                        sharedData.UpdateAutoTestStatus(targetSlot, test_id, "PASS");
                    }
                    
                    Console.WriteLine($"[API] Auto test {test_id} completed successfully");
                    return true;
                }
                else
                {
                    // 테스트 상태 업데이트
                    if (test_id < sharedData.GetDeviceByIndex(targetSlot)?.PeripheralData.AutoTests.Count)
                    {
                        sharedData.UpdateAutoTestStatus(targetSlot, test_id, "FAIL");
                    }
                    
                    Console.WriteLine($"[API] Auto test {test_id} failed: {response.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to run auto test: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 수동 테스트 실행
        /// </summary>
        /// <param name="testId">테스트 ID</param>
        /// <param name="parameters">가변 파라미터</param>
        /// <returns>테스트 성공 여부</returns>
        public async Task<bool> DoManualTest(int testId, params object[] parameters)
        {
            try
            {
                Console.WriteLine($"[API] Running manual test ID: {testId} with {parameters.Length} parameters");
                
                // 연결된 디바이스 찾기
                var targetSlot = -1;
                for (int i = 0; i < 4; i++)
                {
                    var device = sharedData.GetDeviceByIndex(i);
                    if (device != null && device.IsConnected)
                    {
                        targetSlot = i;
                        break;
                    }
                }
                
                if (targetSlot < 0)
                {
                    Console.WriteLine("[API] No connected devices found for manual test");
                    return false;
                }
                
                // UnifiedMqttManager를 통해 테스트 명령 전송
                var testData = new
                {
                    testType = "manual",
                    testId = testId,
                    parameters = parameters,
                    timestamp = DateTime.Now
                };
                
                var response = await mqttManager.SendToDevice(targetSlot, "run_manual_test", testData);
                
                if (response.Status == "OK" || response.Status == "PASS")
                {
                    // 테스트 상태 업데이트
                    if (testId < sharedData.GetDeviceByIndex(targetSlot)?.PeripheralData.ManualTests.Count)
                    {
                        sharedData.UpdateManualTestStatus(targetSlot, testId, "PASS");
                    }
                    
                    Console.WriteLine($"[API] Manual test {testId} completed successfully");
                    return true;
                }
                else
                {
                    // 테스트 상태 업데이트
                    if (testId < sharedData.GetDeviceByIndex(targetSlot)?.PeripheralData.ManualTests.Count)
                    {
                        sharedData.UpdateManualTestStatus(targetSlot, testId, "FAIL");
                    }
                    
                    Console.WriteLine($"[API] Manual test {testId} failed: {response.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to run manual test: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 테스트 결과 저장
        /// </summary>
        /// <param name="saveTarget">저장 대상 (1: 로컬파일, 2: DB, 3: 안드로이드 기기)</param>
        /// <param name="testResult">테스트 결과</param>
        /// <param name="parameters">추가 파라미터</param>
        /// <returns>저장된 파일 경로</returns>
        public string SaveResult(int saveTarget, object testResult, params object[] parameters)
        {
            try
            {
                // TODO: 실제 테스트 결과 저장 로직 구현
                string filePath = $"test_result_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                Console.WriteLine($"Saving test result to target {saveTarget}: {filePath}");
                return filePath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save test result: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// 테스트 결과 삭제
        /// </summary>
        /// <param name="saveTarget">저장 대상 (1: 로컬파일, 2: DB, 3: 안드로이드 기기)</param>
        /// <param name="filePath">파일 경로 (target 1,3에서 사용)</param>
        /// <param name="model">모델 정보 (target 2에서 사용)</param>
        /// <param name="version">버전 정보 (target 2에서 사용)</param>
        /// <returns>삭제 성공 여부</returns>
        public bool RemoveResult(int saveTarget, string filePath, string model = null, int version = 0)
        {
            try
            {
                // TODO: 실제 테스트 결과 삭제 로직 구현
                switch (saveTarget)
                {
                    case 1: // 로컬파일
                    case 3: // 안드로이드 기기
                        Console.WriteLine($"Removing result file: {filePath}");
                        break;
                    case 2: // DB
                        Console.WriteLine($"Removing result from DB - Model: {model}, Version: {version}");
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to remove test result: {ex.Message}");
                return false;
            }
        }
        
        #endregion

        #region 파일 관련 함수
        
        /// <summary>
        /// 안드로이드 기기에서 이미지 파일 가져오기
        /// </summary>
        /// <param name="filePath">파일 경로</param>
        /// <returns>이미지 파일 데이터 또는 로컬 경로</returns>
        public async Task<string> GetImageFile(string filePath)
        {
            try
            {
                // 연결된 디바이스 확인
                var connectedDevices = await adbMonitor.GetConnectedDevicesAsync();
                if (connectedDevices.Count == 0)
                {
                    Console.WriteLine("[API] No connected devices found for file transfer");
                    return null;
                }
                
                // 첫 번째 연결된 디바이스에서 파일 가져오기
                var targetDevice = connectedDevices[0];
                var localFileName = $"pulled_{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(filePath)}";
                var localPath = Path.Combine(Path.GetTempPath(), localFileName);
                
                Console.WriteLine($"[API] Pulling file from {targetDevice.DeviceId}: {filePath} -> {localPath}");
                
                var processInfo = new ProcessStartInfo
                {
                    FileName = "adb",
                    Arguments = $"-s {targetDevice.DeviceId} pull \"{filePath}\" \"{localPath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    Console.WriteLine("[API] Failed to start ADB process for file transfer");
                    return null;
                }
                
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                if (process.ExitCode == 0 && File.Exists(localPath))
                {
                    Console.WriteLine($"[API] File transfer successful: {localPath}");
                    return localPath;
                }
                else
                {
                    Console.WriteLine($"[API] File transfer failed: {error}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Failed to get image file: {ex.Message}");
                return null;
            }
        }
        
        #endregion

        #region 유틸리티 메서드
        
        /// <summary>
        /// API 상태 확인 (내부 사용)
        /// </summary>
        /// <returns>API 사용 가능 여부</returns>
        internal bool IsApiReady()
        {
            // 실제 API 상태 확인 로직 구현
            return true;
        }
        
        /// <summary>
        /// 리소스 정리 (내부 사용)
        /// </summary>
        internal async Task Dispose()
        {
            try
            {
                Console.WriteLine("[API] Starting resource cleanup...");
                
                // ADB 모니터링 중지
                if (adbMonitor != null)
                {
                    await adbMonitor.StopMonitoringAsync();
                    adbMonitor.Dispose();
                    Console.WriteLine("[API] ADB monitor disposed");
                }
                
                // MQTT 연결 해제 (시뮬레이션 모드로 전환)
                if (mqttManager != null)
                {
                    mqttManager.SwitchToSimulationMode();
                    Console.WriteLine("[API] MQTT manager switched to simulation mode");
                }
                
                // 이벤트 핸들러 정리
                OnDeviceEvent = null;
                OnDatabaseEvent = null;
                OnMessageReceived = null;
                
                // 디바이스 토픽 정리
                deviceTopics.Clear();
                
                Console.WriteLine("[API] Resource cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API] Error during resource cleanup: {ex.Message}");
            }
        }
        
        #endregion
    }
} 