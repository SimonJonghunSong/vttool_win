using System;
using System.Windows.Forms;
using AndroidDeviceVerificationTool.Data;
using AndroidDeviceVerificationTool.Controls;

namespace AndroidDeviceVerificationTool
{
    /// <summary>
    /// 🎯 통합된 UnifiedDeviceData + TestConnectData 사용 예제
    /// PostSetupData → ApnInfo 대체 완료
    /// </summary>
    public static class UnifiedDataExample
    {
        /// <summary>
        /// 새로운 통합 구조 사용 예제
        /// </summary>
        public static void DemonstrateUnifiedStructure()
        {
            var sharedData = SharedApplicationData.Instance;
            
            // 🚀 기본 디바이스 4개 설정
            sharedData.SetupBasicDevices();
            
            Console.WriteLine("=== 🎯 UnifiedDeviceData + TestConnectData 통합 구조 예제 ===\n");
            
            // 📱 첫 번째 디바이스로 예제 실행
            var device = sharedData.GetDeviceByIndex(0);
            if (device != null)
            {
                Console.WriteLine($"📱 디바이스: {device.Name} ({device.DeviceId})");
                Console.WriteLine($"🔗 연결 상태: {device.IsConnected}");
                
                // 🎯 TestConnectData 접근 (통합된 구조)
                var testConnectData = device.TestConnectData;
                if (testConnectData != null)
                {
                    Console.WriteLine($"\n🧪 테스트 항목 수: {testConnectData.TestItems.Count}");
                    Console.WriteLine($"📊 선택된 테스트: {testConnectData.GetSelectedTestCount()}개");
                    
                    // 🌐 APN 정보 설정 (PostSetupData 대체)
                    device.ApnInfo.APN = "internet.example.com";
                    device.ApnInfo.UserID = "user123";
                    device.ApnInfo.Password = "pass456";
                    
                    Console.WriteLine($"\n🌐 APN 정보:");
                    Console.WriteLine($"  - APN: {device.ApnInfo.APN}");
                    Console.WriteLine($"  - User ID: {device.ApnInfo.UserID}");
                    Console.WriteLine($"  - Password: {device.ApnInfo.Password}");
                    
                    // 🗄️ DB 정보 설정
                    testConnectData.DatabaseInfo.IP = "192.168.1.100";
                    testConnectData.DatabaseInfo.Port = "3306";
                    testConnectData.DatabaseInfo.ID = "admin";
                    testConnectData.DatabaseInfo.Password = "dbpass";
                    
                    Console.WriteLine($"\n🗄️ DB 정보:");
                    Console.WriteLine($"  - 연결: {testConnectData.DatabaseInfo.IP}:{testConnectData.DatabaseInfo.Port}");
                    Console.WriteLine($"  - 계정: {testConnectData.DatabaseInfo.ID}");
                    
                    // 📱 APK 정보 설정
                    testConnectData.ApkInfo.FilePath = @"C:\TestApps\TestApp.apk";
                    
                    Console.WriteLine($"\n📱 APK 정보:");
                    Console.WriteLine($"  - 파일: {testConnectData.ApkInfo.FileName}");
                    Console.WriteLine($"  - 경로: {testConnectData.ApkInfo.FilePath}");
                    Console.WriteLine($"  - 유효: {testConnectData.ApkInfo.IsValid}");
                    
                    // 📶 무선 정보 설정
                    testConnectData.WirelessInfo.WifiSSID = "TestWiFi";
                    testConnectData.WirelessInfo.BleName = "TestBLE";
                    
                    Console.WriteLine($"\n📶 무선 정보:");
                    Console.WriteLine($"  - Wi-Fi: {testConnectData.WirelessInfo.WifiSSID}");
                    Console.WriteLine($"  - BLE: {testConnectData.WirelessInfo.BleName}");
                }
            }
            
            Console.WriteLine("\n=== 🔧 SharedApplicationData API 사용 ===");
            
            // 🌐 APN 데이터 API 사용 (PostSetupData 대체)
            sharedData.SetApnData(0, "new.apn.com", "newuser", "newpass");
            var (apn, userId, password) = sharedData.GetApnData(0);
            Console.WriteLine($"\n🌐 APN API 결과:");
            Console.WriteLine($"  - APN: {apn}");
            Console.WriteLine($"  - User ID: {userId}");
            Console.WriteLine($"  - Password: {password}");
            
            // 🎯 TestConnect 데이터 전체 가져오기
            var retrievedData = sharedData.GetTestConnectData(0);
            if (retrievedData != null)
            {
                Console.WriteLine($"\n📊 TestConnect 요약:");
                Console.WriteLine(retrievedData.GetSummary());
                
                // ✅ 데이터 검증
                var validation = retrievedData.Validate();
                Console.WriteLine($"\n✅ 검증 결과:");
                Console.WriteLine(validation.GetSummary());
            }
        }
        
        /// <summary>
        /// TestConnectSuccessDialog와 통합 사용 예제
        /// </summary>
        public static void DemonstrateDialogIntegration(Form parentForm = null)
        {
            var sharedData = SharedApplicationData.Instance;
            
            // 디바이스가 없으면 생성
            if (sharedData.Devices.Count == 0)
            {
                sharedData.SetupBasicDevices();
            }
            
            var device = sharedData.GetDeviceByIndex(0);
            if (device != null)
            {
                Console.WriteLine("🎯 TestConnectSuccessDialog 통합 예제 시작...");
                
                // 🎯 다이얼로그 표시
                using (var dialog = new TestConnectSuccessDialog(device))
                {
                    var result = dialog.ShowDialog(parentForm);
                    
                    if (result == DialogResult.OK)
                    {
                        // ✅ 변경사항이 자동으로 UnifiedDeviceData에 저장됨
                        var updatedData = device.TestConnectData;
                        
                        Console.WriteLine("✅ 다이얼로그 완료 - 저장된 데이터:");
                        Console.WriteLine($"📊 선택된 테스트: {updatedData.GetSelectedTestCount()}개");
                        
                        Console.WriteLine($"🗄️ DB: {updatedData.DatabaseInfo.IP}:{updatedData.DatabaseInfo.Port}");
                        Console.WriteLine($"📱 APK: {updatedData.ApkInfo.FileName}");
                        
                        // 🔧 SharedApplicationData를 통해서도 접근 가능
                        var (apn, userId, pass) = sharedData.GetApnData(0);
                        Console.WriteLine($"🔧 SharedData APN: {apn} ({userId})");
                    }
                    else
                    {
                        Console.WriteLine("❌ 다이얼로그 취소됨");
                    }
                }
            }
        }
        
        /// <summary>
        /// 동적 테스트 관리 예제
        /// </summary>
        public static void DemonstrateDynamicTestManagement()
        {
            var sharedData = SharedApplicationData.Instance;
            var device = sharedData.GetDeviceByIndex(0);
            
            if (device?.TestConnectData != null)
            {
                var testData = device.TestConnectData;
                
                Console.WriteLine("🧪 동적 테스트 관리 예제:");
                Console.WriteLine($"초기 테스트 수: {testData.TestItems.Count}");
                
                // 🔧 테스트 추가
                testData.AddTestItem("Custom Test 1", isSelected: true);
                testData.AddTestItem("Custom Test 2", isSelected: false);
                Console.WriteLine($"추가 후: {testData.TestItems.Count}개");
                
                // 🔍 선택된 테스트 확인
                Console.WriteLine($"선택된 테스트: {testData.GetSelectedTestCount()}개");
                
                // 🗑️ 테스트 제거
                testData.RemoveTestItem("Custom Test 2");
                Console.WriteLine($"제거 후: {testData.TestItems.Count}개");
                
                // 📊 모든 테스트 선택
                testData.SetAllTestItemsSelected(true);
                Console.WriteLine($"전체 선택 후: {testData.GetSelectedTestCount()}개 선택");
            }
        }
        
        /// <summary>
        /// 메인 폼에서 호출할 수 있는 통합 예제 메뉴
        /// </summary>
        public static ToolStripMenuItem CreateUnifiedExampleMenuItem()
        {
            var menuItem = new ToolStripMenuItem("🎯 Unified Data Structure Example");
            
            var demoStructure = new ToolStripMenuItem("📊 구조 데모");
            demoStructure.Click += (s, e) => DemonstrateUnifiedStructure();
            
            var demoDialog = new ToolStripMenuItem("🎯 다이얼로그 통합");
            demoDialog.Click += (s, e) => DemonstrateDialogIntegration();
            
            var demoTests = new ToolStripMenuItem("🧪 동적 테스트 관리");
            demoTests.Click += (s, e) => DemonstrateDynamicTestManagement();
            
            menuItem.DropDownItems.Add(demoStructure);
            menuItem.DropDownItems.Add(demoDialog);
            menuItem.DropDownItems.Add(demoTests);
            
            return menuItem;
        }
    }
} 