using System;
using AndroidDeviceVerificationTool.Data;

namespace AndroidDeviceVerificationTool.Examples
{
    /// <summary>
    /// AndroidDeviceAPI 사용 예제
    /// </summary>
    public class AndroidDeviceAPIExample
    {
        private AndroidDeviceAPI api;
        
        public AndroidDeviceAPIExample()
        {
            // API 인스턴스 가져오기 (싱글톤 패턴)
            api = AndroidDeviceAPI.Instance;
            
            // 콜백 함수들 등록
            SetupCallbacks();
        }
        
        /// <summary>
        /// 콜백 함수들 설정
        /// </summary>
        private void SetupCallbacks()
        {
            // 디바이스 이벤트 콜백 등록 (api.txt 함수명 사용)
            api.cb_dev_evt(OnDeviceEvent);
            
            // 데이터베이스 이벤트 콜백 등록  
            api.cb_db_evt(OnDatabaseEvent);
            
            // 메시지 수신 콜백 등록
            api.cb_recv_msg(OnMessageReceived);
        }
        
        /// <summary>
        /// 디바이스 이벤트 콜백 함수
        /// </summary>
        /// <param name="parameters">가변 파라미터</param>
        private void OnDeviceEvent(params object[] parameters)
        {
            Console.WriteLine("OnDeviceEvent...");

            Console.WriteLine($"[Device Event] Received {parameters.Length} parameters");
            for (int i = 0; i < parameters.Length; i++)
            {
                Console.WriteLine($"  Parameter {i}: {parameters[i]}");
            }
        }
        
        /// <summary>
        /// 데이터베이스 이벤트 콜백 함수
        /// </summary>
        /// <param name="parameters">가변 파라미터</param>
        private void OnDatabaseEvent(params object[] parameters)
        {
            if (parameters.Length > 0)
            {
                string status = parameters[0].ToString();
                Console.WriteLine($"[Database Event] Status: {status}");
                
                if (parameters.Length >= 3)
                {
                    string ip = parameters[1].ToString();
                    int port = Convert.ToInt32(parameters[2]);
                    Console.WriteLine($"  Connection: {ip}:{port}");
                }
            }
        }
        
        /// <summary>
        /// 메시지 수신 콜백 함수
        /// </summary>
        /// <param name="msgName">메시지 이름</param>
        /// <param name="parameters">가변 파라미터</param>
        private void OnMessageReceived(string msgName, params object[] parameters)
        {
            Console.WriteLine($"[Message Received] {msgName} with {parameters.Length} parameters");
            for (int i = 0; i < parameters.Length; i++)
            {
                Console.WriteLine($"  Parameter {i}: {parameters[i]}");
            }
        }
        
        /// <summary>
        /// 앱 관리 예제
        /// </summary>
        public async Task AppManagementExample()
        {
            Console.WriteLine("=== App Management Example ===");
            
            string apkPath = @"C:\test\example.apk";
            
            // APK 설치 (api.txt 함수명 사용)
            bool installResult = await api.install_app(apkPath);
            Console.WriteLine($"Install result: {installResult}");
            
            // APK 제거 (패키지명으로)
            bool removeResult = await api.remove_app("com.example.testapp");
            Console.WriteLine($"Remove result: {removeResult}");
        }
        
        /// <summary>
        /// 디바이스 감지 예제
        /// </summary>
        public async Task DeviceDetectionExample()
        {
            Console.WriteLine("=== Device Detection Example ===");
            
            // 디바이스 감지 시작 (api.txt 함수명 사용)
            string recvTopic = await api.start_dev_detect();
            Console.WriteLine($"Detection started with topic: {recvTopic}");
            
            // 잠시 대기 (실제로는 디바이스 감지 로직이 실행됨)
            await Task.Delay(2000);
            
            // 디바이스 감지 중지
            await api.stop_dev_detect(recvTopic);
        }
        
        /// <summary>
        /// MQTT 관련 예제
        /// </summary>
        public async Task MqttExample()
        {
            Console.WriteLine("=== MQTT Example ===");
            
            string recvTopic = "device/status";
            string subTopic = "device/commands";
            string pubTopic = "device/responses";
            
            // MQTT 수신 채널 열기 (api.txt 함수명 사용)
            bool openResult = await api.mqtt_recv_open(recvTopic);
            Console.WriteLine($"MQTT receive open result: {openResult}");
            
            // MQTT 조인
            bool joinResult = await api.mqtt_join(subTopic, pubTopic);
            Console.WriteLine($"MQTT join result: {joinResult}");
            
            // 메시지 전송
            bool sendResult = api.send_msg("test_message", "param1", 123, true);
            Console.WriteLine($"Message send result: {sendResult}");
            
            // MQTT 수신 채널 닫기
            bool closeResult = api.mqtt_recv_close(recvTopic);
            Console.WriteLine($"MQTT receive close result: {closeResult}");
        }
        
        /// <summary>
        /// 데이터베이스 연결 예제
        /// </summary>
        public async Task DatabaseExample()
        {
            Console.WriteLine("=== Database Example ===");
            
            string ip = "192.168.1.100";
            int port = 3306;
            string password = "mypassword";
            
            // 데이터베이스 연결 열기
            bool openResult = api.OpenDatabaseConnection(ip, port, password);
            Console.WriteLine($"Database connection open result: {openResult}");
            
            // 잠시 대기
            await Task.Delay(1000);
            
            // 데이터베이스 연결 닫기
            bool closeResult = api.CloseDatabaseConnection(ip, port, password);
            Console.WriteLine($"Database connection close result: {closeResult}");
        }
        
        /// <summary>
        /// 테스트 관련 예제
        /// </summary>
        public async Task TestExample()
        {
            Console.WriteLine("=== Test Example ===");
            
            // 자동 테스트 실행 (api.txt 함수명 사용)
            bool autoTestResult = await api.do_auto_test(101, "test_param1", 456, true);
            Console.WriteLine($"Auto test result: {autoTestResult}");
            
            // 수동 테스트 실행
            bool manualTestResult = await api.DoManualTest(201, "manual_param1", 789);
            Console.WriteLine($"Manual test result: {manualTestResult}");
            
            // 테스트 결과 저장 (로컬 파일)
            string savedPath1 = api.SaveResult(1, "Test completed successfully", "additional_info");
            Console.WriteLine($"Test result saved to: {savedPath1}");
            
            // 테스트 결과 저장 (데이터베이스)
            string savedPath2 = api.SaveResult(2, "DB test result", "model_info", "version_info");
            Console.WriteLine($"Test result saved to DB: {savedPath2}");
            
            // 테스트 결과 삭제 (로컬 파일)
            bool removeResult1 = api.RemoveResult(1, savedPath1);
            Console.WriteLine($"Local file remove result: {removeResult1}");
            
            // 테스트 결과 삭제 (데이터베이스)
            bool removeResult2 = api.RemoveResult(2, null, "TestModel", 1);
            Console.WriteLine($"DB record remove result: {removeResult2}");
        }
        
        /// <summary>
        /// 설정 및 파일 관련 예제
        /// </summary>
        public async Task ConfigAndFileExample()
        {
            Console.WriteLine("=== Config and File Example ===");
            
            // 설정 정보 가져오기 (api.txt 함수명 사용)
            string config = api.get_config();
            Console.WriteLine($"Current config: {config}");
            
            // 설정 정보 설정
            await api.set_config("db_connection_string", "MyWiFi_SSID", "MyBLE_Device");
            
            // 이미지 파일 가져오기
            string imagePath = "/sdcard/Pictures/screenshot.png";
            string localImagePath = await api.GetImageFile(imagePath);
            Console.WriteLine($"Image file copied to: {localImagePath}");
        }
        
        /// <summary>
        /// ADB 포트 포워딩 예제
        /// </summary>
        public async Task AdbPortForwardingExample()
        {
            Console.WriteLine("=== ADB Port Forwarding Example ===");
            
            int port = 8080;
            //bool result = await api.OpenAdbPortForwarding(port);
            //Console.WriteLine($"ADB port forwarding result: {result}");
        }
        
        /// <summary>
        /// 전체 워크플로우 예제
        /// </summary>
        public async Task FullWorkflowExample()
        {
            Console.WriteLine("=== Full Workflow Example ===");
            
            try
            {
                //// 1. API 상태 확인
                //if (!api.IsApiReady())
                //{
                //    Console.WriteLine("API is not ready!");
                //    return;
                //}
                
                //// 2. 디바이스 감지 시작
                //string detectionTopic = await api.StartDeviceDetect();
                
                //// 3. ADB 포트 포워딩 설정
                //await api.OpenAdbPortForwarding(5555);
                
                //// 4. MQTT 연결 설정
                //await api.MqttJoin("device/sub", "device/pub");
                
                //// 5. 데이터베이스 연결
                //bool dbResult = api.OpenDatabaseConnection("localhost", 3306, "password");
                
                //// 6. APK 설치
                //await api.InstallApp(@"C:\test\testapp.apk");
                
                //// 7. 자동 테스트 실행
                //await api.DoAutoTest(100, "workflow_test");
                
                //// 8. 테스트 결과 저장
                //string resultPath = api.SaveResult(1, "Workflow test completed");
                
                //// 9. 정리 작업
                //await api.StopDeviceDetect(detectionTopic);
                //bool closeResult = api.CloseDatabaseConnection("localhost", 3306, "password");
                
                Console.WriteLine("Full workflow completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Workflow failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 모든 예제 실행
        /// </summary>
        public async Task RunAllExamples()
        {
            Console.WriteLine("Starting AndroidDeviceAPI Examples...\n");
            
            await AppManagementExample();
            Console.WriteLine();
            
            await DeviceDetectionExample();
            Console.WriteLine();
            
            await MqttExample();
            Console.WriteLine();
            
            await DatabaseExample();
            Console.WriteLine();
            
            await TestExample();
            Console.WriteLine();
            
            await ConfigAndFileExample();
            Console.WriteLine();
            
            await AdbPortForwardingExample();
            Console.WriteLine();
            
            await FullWorkflowExample();
            Console.WriteLine();
            
            Console.WriteLine("All examples completed!");
        }
        
        /// <summary>
        /// 리소스 정리
        /// </summary>
        public async Task Cleanup()
        {
            if (api != null)
            {
                await api.Dispose();
            }
        }
    }
} 