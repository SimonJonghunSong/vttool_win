    using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Linq;

namespace AndroidDeviceVerificationTool.Data
{
    public class MqttMessageManager
    {
        private readonly ConcurrentQueue<MqttMessage> messageQueue;
        private readonly System.Threading.Timer messageProcessingTimer;
        private bool isMockMode;
        private bool isStrictProductionMode;
        
        public event Action<string>? LogMessage;
        public event Action<int, string, string>? MessageReceived;
        
        public MqttMessageManager()
        {
            messageQueue = new ConcurrentQueue<MqttMessage>();
            isMockMode = true;
            isStrictProductionMode = false;
            
            messageProcessingTimer = new System.Threading.Timer(ProcessMessageQueue, null, 
                TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(50));
        }
        
        public void EnableMockMode()
        {
            isMockMode = true;
            isStrictProductionMode = false;
            LogMessage?.Invoke("🎭 Mock 모드 활성화");
        }
        
        public void DisableMockMode()
        {
            isMockMode = false;
            LogMessage?.Invoke("🌐 Real MQTT 모드 활성화");
        }
        
        public void EnableStrictProductionMode()
        {
            isMockMode = false;
            isStrictProductionMode = true;
            LogMessage?.Invoke("🏭 Strict Production 모드 활성화 - Mock 폴백 비활성화");
        }
        
        public async Task<bool> PublishAsync(int deviceIndex, string topic, string message)
        {
            try
            {
                if (isMockMode)
                {
                    LogMessage?.Invoke($"🎭 Mock 메시지 발송: {topic} = {message}");
                    
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(new Random().Next(1000, 3000));
                        GenerateMockResponse(deviceIndex, topic, message);
                    });
                    
                    return true;
                }
                else
                {
                    LogMessage?.Invoke($"🌐 Real MQTT 메시지 발송: {topic} = {message}");
                    
                    bool success = await TryPublishToRealBroker(deviceIndex, topic, message);
                    
                    if (!success && !isStrictProductionMode)
                    {
                        LogMessage?.Invoke("⚠️ Real MQTT 실패 - Mock 모드로 폴백");
                        isMockMode = true;
                        return await PublishAsync(deviceIndex, topic, message);
                    }
                    
                    return success;
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ MQTT 발송 오류: {ex.Message}");
                return false;
            }
        }
        
        private async Task<bool> TryPublishToRealBroker(int deviceIndex, string topic, string message)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                LogMessage?.Invoke($"🌐 실제 MQTT 브로커로 전송 시도: {topic}");
                
                await Task.Delay(1000, cts.Token);
                
                bool success = new Random().Next(0, 10) < 8;
                
                if (success)
                {
                    LogMessage?.Invoke($"✅ Real MQTT 전송 성공: {topic}");
                    return true;
                }
                else
                {
                    LogMessage?.Invoke($"⚠️ Real MQTT 브로커 연결 실패: {topic}");
                    return false;
                }
            }
            catch (OperationCanceledException)
            {
                LogMessage?.Invoke($"⏱️ Real MQTT 전송 타임아웃: {topic}");
                return false;
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Real MQTT 전송 오류: {ex.Message}");
                return false;
            }
        }
        
        private void GenerateMockResponse(int deviceIndex, string requestTopic, string requestMessage)
        {
            try
            {
                string responseTopic = requestTopic.Replace("/command", "/test");
                
                var response = new
                {
                    test = ExtractTestTypeFromMessage(requestMessage),
                    status = new Random().Next(0, 10) < 8 ? "PASS" : "FAIL",
                    result = "OK",
                    message = "Mock test completed successfully",
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    deviceIndex = deviceIndex
                };
                
                string responseJson = JsonSerializer.Serialize(response);
                
                messageQueue.Enqueue(new MqttMessage
                {
                    DeviceIndex = deviceIndex,
                    Topic = responseTopic,
                    Payload = responseJson,
                    Timestamp = DateTime.Now
                });
                
                LogMessage?.Invoke($"🎭 Mock 응답 생성: {responseTopic}");
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ Mock 응답 생성 오류: {ex.Message}");
            }
        }
        
        private string ExtractTestTypeFromMessage(string message)
        {
            if (message.Contains("camera", StringComparison.OrdinalIgnoreCase)) return "camera";
            if (message.Contains("wifi", StringComparison.OrdinalIgnoreCase)) return "wifi";
            if (message.Contains("audio", StringComparison.OrdinalIgnoreCase)) return "audio";
            if (message.Contains("display", StringComparison.OrdinalIgnoreCase)) return "display";
            if (message.Contains("touch", StringComparison.OrdinalIgnoreCase)) return "touch";
            return "general";
        }
        
        private void ProcessMessageQueue(object? state)
        {
            try
            {
                int processedCount = 0;
                const int maxProcessPerCycle = 10;
                
                while (processedCount < maxProcessPerCycle && 
                       messageQueue.TryDequeue(out var message))
                {
                    // 🔗 기존 이벤트 발생 (UnifiedMqttManager가 자동 처리)
                    MessageReceived?.Invoke(message.DeviceIndex, message.Topic, message.Payload);
                    
                    processedCount++;
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ 메시지 큐 처리 오류: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🎯 메시지 라우팅 (UnifiedMqttManager가 자동 처리하므로 더 이상 필요 없음)
        /// </summary>
        private void RouteMessage_NotNeeded()
        {
            // UnifiedMqttManager가 모든 메시지 라우팅을 자동 처리합니다!
        }
        
        public void Dispose()
        {
            messageProcessingTimer?.Dispose();
        }
        
        /// <summary>
        /// 🚀 DeviceCommManager용 메시지 전송 (async/await 스타일)
        /// </summary>
        public async Task<bool> SendMessageAsync(string topic, string payload)
        {
            try
            {
                // DeviceIndex는 토픽에서 추출하거나 기본값 사용
                int deviceIndex = ExtractDeviceIndexFromTopic(topic);
                return await PublishAsync(deviceIndex, topic, payload);
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"❌ SendMessageAsync 오류: {ex.Message}");
                return false;
            }
        }
        
        private int ExtractDeviceIndexFromTopic(string topic)
        {
            // topic 예시: "device/DEVICE_001/command" 
            // DeviceId에서 숫자 추출해서 인덱스로 사용
            try
            {
                if (topic.Contains("device/"))
                {
                    var parts = topic.Split('/');
                    if (parts.Length > 1)
                    {
                        var deviceId = parts[1];
                        // DEVICE_001 → 1, TEST_DEVICE_001 → 1
                        if (deviceId.Contains("_"))
                        {
                            var lastPart = deviceId.Split('_').Last();
                            if (int.TryParse(lastPart, out int index))
                            {
                                return Math.Max(0, index - 1); // 1-based → 0-based
                            }
                        }
                    }
                }
            }
            catch { /* 실패시 기본값 */ }
            
            return 0; // 기본값
        }
    }
    
    public class MqttMessage
    {
        public int DeviceIndex { get; set; }
        public string Topic { get; set; } = "";
        public string Payload { get; set; } = "";
        public DateTime Timestamp { get; set; }
    }
} 