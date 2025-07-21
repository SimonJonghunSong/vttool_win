using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// 🎭 **Mock MQTT 디바이스**
    /// - MQTT 브로커 없이 가상 테스트
    /// - 실제 디바이스 동작 시뮬레이션  
    /// - 안전한 연결/해제 관리
    /// </summary>
    public class MockMqttDevice
    {
        private readonly int deviceIndex;
        private readonly string deviceId;
        private CancellationTokenSource? cancellationTokenSource;
        private readonly Random random = new Random();
        private bool isSimulationMode = false;
        
        public event Action<string>? LogMessage;
        public event Action<string, string>? MessageReceived; // 🎭 가상 메시지 수신용
        
        public MockMqttDevice(int deviceIndex, string deviceId)
        {
            this.deviceIndex = deviceIndex;
            this.deviceId = deviceId;
        }
        
        /// <summary>
        /// 🎭 시뮬레이션 모드 시작 (브로커 없이 가상 동작)
        /// </summary>
        public async Task StartSimulationAsync()
        {
            try
            {
                isSimulationMode = true;
                cancellationTokenSource = new CancellationTokenSource();
                
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} simulation started (no broker required)");
                
                // 가상 heartbeat 시작
                StartVirtualHeartbeat(cancellationTokenSource.Token);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} simulation start error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🎭 시뮬레이션 모드 중지
        /// </summary>
        public async Task StopSimulationAsync()
        {
            try
            {
                isSimulationMode = false;
                cancellationTokenSource?.Cancel();
                
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} simulation stopped");
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} simulation stop error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🎭 가상 메시지 처리 (MQTT 브로커 없이)
        /// </summary>
        public async Task ProcessVirtualCommandAsync(string command)
        {
            try
            {
                if (!isSimulationMode)
                {
                    LogMessage?.Invoke($"🎭 Device #{deviceIndex} not in simulation mode");
                    return;
                }
                
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} received virtual command: {command}");
                
                // 1-3초 가상 지연
                await Task.Delay(random.Next(1000, 3000));
                
                var response = GenerateTestResponse(command);
                string responseJson = JsonSerializer.Serialize(response);
                string responseTopic = $"device/{deviceIndex}/test";
                
                // 가상 응답 전송
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} sending virtual response to topic: {responseTopic}");
                LogMessage?.Invoke($"🎭 Response JSON: {responseJson}");
                MessageReceived?.Invoke(responseTopic, responseJson);
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} virtual response sent successfully!");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} virtual command error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🧪 테스트 결과 생성
        /// </summary>
        private MockTestResponse GenerateTestResponse(string command)
        {
            var testTypes = new[] { "camera", "wifi", "audio", "display", "touch" };
            var testType = testTypes[random.Next(testTypes.Length)];
            
            if (command.Contains("camera")) testType = "camera";
            else if (command.Contains("wifi")) testType = "wifi";
            else if (command.Contains("audio")) testType = "audio";
            else if (command.Contains("display")) testType = "display";
            else if (command.Contains("touch")) testType = "touch";
            
            bool isPass = random.Next(0, 10) < 8; // 80% 성공률
            
            var response = new MockTestResponse
            {
                test = testType,
                status = isPass ? "PASS" : "FAIL",
                result = isPass ? "OK" : "ERROR",
                message = GenerateTestMessage(testType, isPass),
                timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                deviceId = deviceId
            };
            
            if (!isPass)
            {
                response.errorCode = random.Next(1001, 1999);
                response.errorDetail = GenerateErrorDetail(testType);
            }
            
            return response;
        }
        
        private string GenerateTestMessage(string testType, bool isPass)
        {
            if (isPass)
            {
                return testType switch
                {
                    "camera" => "Camera test completed successfully. All functions working.",
                    "wifi" => "WiFi connection established. Signal strength: Good.",
                    "audio" => "Audio playback and recording working properly.",
                    "display" => "Display test passed. All pixels responding.",
                    "touch" => "Touch screen calibration successful.",
                    _ => "Test completed successfully."
                };
            }
            else
            {
                return testType switch
                {
                    "camera" => "Camera module not responding to initialization commands.",
                    "wifi" => "WiFi adapter not found or driver issue detected.",
                    "audio" => "Audio codec initialization failed or hardware fault.",
                    "display" => "Display panel has dead pixels or connection issues.",
                    "touch" => "Touch sensor calibration failed or hardware malfunction.",
                    _ => "Test failed with unknown error."
                };
            }
        }
        
        private string GenerateErrorDetail(string testType)
        {
            return testType switch
            {
                "camera" => "Camera I2C communication timeout",
                "wifi" => "WiFi chip firmware not responding",
                "audio" => "Audio codec register read failed",
                "display" => "LCD panel MIPI signal error", 
                "touch" => "Touch controller I2C address not found",
                _ => "Hardware communication error"
            };
        }
        
        /// <summary>
        /// 🎭 가상 heartbeat (실제 MQTT 없이)
        /// </summary>
        private void StartVirtualHeartbeat(CancellationToken cancellationToken)
        {
            Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested && isSimulationMode)
                {
                    try
                    {
                        var status = new
                        {
                            deviceId = deviceId,
                            status = "online",
                            battery = random.Next(20, 100),
                            temperature = random.Next(25, 45),
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        
                        string statusJson = JsonSerializer.Serialize(status);
                        string statusTopic = $"device/{deviceIndex}/status";
                        
                        MessageReceived?.Invoke(statusTopic, statusJson);
                        
                        await Task.Delay(10000, cancellationToken); // 10초마다
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        LogMessage?.Invoke($"🎭 Mock Device #{deviceIndex} virtual heartbeat error: {ex.Message}");
                    }
                }
            }, cancellationToken);
        }
        
        public void Dispose()
        {
            if (isSimulationMode)
            {
                Task.Run(async () => await StopSimulationAsync()).Wait(1000);
            }
        }
    }
    
    /// <summary>
    /// Mock 테스트 응답 데이터 구조
    /// </summary>
    public class MockTestResponse
    {
        public string test { get; set; } = "";
        public string status { get; set; } = "";
        public string result { get; set; } = "";
        public string message { get; set; } = "";
        public string timestamp { get; set; } = "";
        public string deviceId { get; set; } = "";
        public int? errorCode { get; set; }
        public string? errorDetail { get; set; }
    }
} 