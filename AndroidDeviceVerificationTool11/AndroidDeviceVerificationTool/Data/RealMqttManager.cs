using System;
using System.Text;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Threading;

namespace AndroidDeviceVerificationTool.Data
{
    /// <summary>
    /// 🌐 실제 MQTT 브로커 연결 매니저
    /// M2Mqtt 패키지 사용하여 실제 브로커와 통신
    /// </summary>
    public class RealMqttManager : IDisposable
    {
        private MqttClient? mqttClient;
        private readonly ConfigManager config;
        private bool isConnected = false;
        private readonly string clientId;
        
        public event Action<string>? LogMessage;
        public event Action<string, string>? MessageReceived; // topic, payload
        
        public RealMqttManager()
        {
            config = ConfigManager.Instance;
            clientId = $"AndroidDeviceVerificationTool_{Guid.NewGuid():N}";
        }
        
        /// <summary>
        /// 🔗 MQTT 브로커에 연결
        /// </summary>
        public async Task<bool> ConnectAsync()
        {
            try
            {
                var mqttConfig = config.GetConfig().MQTT;

                //mqttConfig.BrokerAddress = "192.168.0.3";
                Console.WriteLine($"MQTT Server Connecting: {mqttConfig.BrokerAddress}:{mqttConfig.Port}");
                // M2Mqtt 클라이언트 생성
                mqttClient = new MqttClient(mqttConfig.BrokerAddress, mqttConfig.Port, false, null, null, MqttSslProtocols.None);
                
                // 이벤트 핸들러 등록
                mqttClient.MqttMsgPublishReceived += OnMessageReceived;
                mqttClient.ConnectionClosed += OnConnectionClosed;
                
                // 연결 (사용자명/암호 있으면 사용)
                byte resultCode;
                if (!string.IsNullOrEmpty(mqttConfig.Username))
                {
                    resultCode = mqttClient.Connect(clientId, mqttConfig.Username, mqttConfig.Password);
                }
                else
                {
                    resultCode = mqttClient.Connect(clientId);
                }
                
                isConnected = (resultCode == MqttMsgConnack.CONN_ACCEPTED);
                
                if (isConnected)
                {
                    Console.WriteLine("MQTT Server Connected");

                    await SubscribeTopicsAsync();
                    return true;
                }
                else
                {
                    LogMessage?.Invoke($"MQTT Connection Failed - result: {resultCode}");
                    Console.WriteLine($"MQTT Connection Failed - result: {resultCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"MQTT Connection Error: {ex.Message}");
                Console.WriteLine($"MQTT Connection Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 📡 기본 토픽들 구독 (🎯 고정 디바이스 매핑 지원)
        /// </summary>
        private async Task SubscribeTopicsAsync()
        {
            try
            {
                if (mqttClient?.IsConnected == true)
                {
                    // 🎯 고정 디바이스 매핑에 따른 토픽들
                    string[] subscribeTopics = {
                        // PC가 수신할 기본 토픽들
                        //"pc/command",           // Android → PC 명령
                        //"pc/response",          // Android → PC 응답  
                        //"device/+/response",    // 모든 디바이스 응답
                        //"device/+/event",       // 모든 디바이스 이벤트
                        
                        // 🎯 고정 디바이스별 토픽들 (사용자 요구사항)
                        //"dev_1/response",       // 디바이스 1 응답 (포트 18831)
                        //"dev_2/response",       // 디바이스 2 응답 (포트 18832)
                        //"dev_3/response",       // 디바이스 3 응답 (포트 18833)
                        //"dev_4/response",       // 디바이스 4 응답 (포트 18834)
                        "dev_1/event",          // 디바이스 1 이벤트
                        "dev_2/event",          // 디바이스 2 이벤트
                        "dev_3/event",          // 디바이스 3 이벤트
                        "dev_4/event",           // 디바이스 4 이벤트
                        "dev_1",          // 디바이스 1 Subs
                        "dev_2",          // 디바이스 2 이벤트
                        "dev_3",          // 디바이스 3 이벤트
                        "dev_4"           // 디바이스 4 이벤트
                    };
                    
                    byte[] qosLevels = new byte[subscribeTopics.Length];
                    for (int i = 0; i < subscribeTopics.Length; i++)
                    {
                        qosLevels[i] = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE;
                    }
                    
                    mqttClient.Subscribe(subscribeTopics, qosLevels);
                    
                    //Console.WriteLine($"🎯 고정 매핑 토픽 구독 완료: {subscribeTopics.Length}개 토픽");
                    //Console.WriteLine($"📋 구독 토픽 목록: {string.Join(", ", subscribeTopics)}");
                    //LogMessage?.Invoke($"Topic Subscribe Done: {string.Join(", ", subscribeTopics)}");
                    Console.WriteLine($"Topic Subscribe Done: {string.Join(", ", subscribeTopics)}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Topic Subscribe Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 📤 메시지 발송
        /// </summary>
        public async Task<bool> PublishAsync(string topic, string payload)
        {
            try
            {
                if (mqttClient?.IsConnected != true)
                {
                    //LogMessage?.Invoke("MQTT NOT connected - retry..");
                    Console.WriteLine("MQTT NOT connected - retry..");
                    if (!await ConnectAsync())
                    {
                        return false;
                    }
                }
                
                byte[] messageBytes = Encoding.UTF8.GetBytes(payload);
                ushort messageId = mqttClient.Publish(topic, messageBytes, MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
                
                //LogMessage?.Invoke($"📤 MQTT 메시지 발송: {topic} (ID: {messageId})");
                //LogMessage?.Invoke($"📝 페이로드: {payload}");
                Console.WriteLine($"[MQTT-Pub] [{topic}], {payload}");
                //Console.WriteLine($"MQTT Msg Publish - Payload: {payload}");

                return true;
            }
            catch (Exception ex)
            {
                //LogMessage?.Invoke($"MQTT Msg Publish Error: {ex.Message}");
                Console.WriteLine($"[MQTT-Pub] Error: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 📥 메시지 수신 이벤트
        /// </summary>
        private void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
        {
            try
            {
                string topic = e.Topic;
                string payload = Encoding.UTF8.GetString(e.Message);
                
                LogMessage?.Invoke($"MQTT Msg Recv: {topic}");
                LogMessage?.Invoke($"MQTT Msg Payload: {payload}");
                //Console.WriteLine($"MQTT Msg Recv: {topic}");
                //Console.WriteLine($"MQTT Msg Payload: {payload}");

                // UnifiedMqttManager로 전달
                MessageReceived?.Invoke(topic, payload);    //OnRealMqttMessageReceived
            }
            catch (Exception ex)
            {
                LogMessage?.Invoke($"MQTT Msg Recv Error: {ex.Message}");
                Console.WriteLine($"MQTT Msg Recv Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 🔗 연결 끊어짐 이벤트
        /// </summary>
        private void OnConnectionClosed(object sender, EventArgs e)
        {
            isConnected = false;
            Console.WriteLine("MQTT Disconnected");
            
            // 자동 재연결 시도
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000); // 5초 대기
                Console.WriteLine("MQTT connection retry!!");
                await ConnectAsync();
            });
        }
        
        /// <summary>
        /// 🔌 연결 해제
        /// </summary>
        public void Disconnect()
        {
            try
            {
                if (mqttClient?.IsConnected == true)
                {
                    mqttClient.Disconnect();
                    Console.WriteLine("MQTT Disconnected");
                }
                isConnected = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"MQTT Disconnecting ERROR. {ex.Message}");
            }
        }
        
        /// <summary>
        /// 📊 연결 상태 확인
        /// </summary>
        public bool IsConnected => isConnected && mqttClient?.IsConnected == true;
        
        /// <summary>
        /// 🧹 리소스 정리
        /// </summary>
        public void Dispose()
        {
            Disconnect();
            // M2Mqtt의 MqttClient에는 Dispose가 없으므로 Disconnect만 호출
        }
    }
} 
