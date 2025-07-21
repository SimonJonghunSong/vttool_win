using System;
using System.Windows.Forms;
using AndroidDeviceVerificationTool.Controls;
using AndroidDeviceVerificationTool.Data;

namespace AndroidDeviceVerificationTool
{
    /// <summary>
    /// 🎯 TestConnectSuccessDialog 사용 예제
    /// </summary>
    public static class TestConnectDialogExample
    {
        /// <summary>
        /// 다이얼로그 표시 예제
        /// </summary>
        public static void ShowExample(Form parentForm = null)
        {
            // 🎯 테스트용 디바이스 생성
            var testDevice = new UnifiedDeviceData("Test Device ST9730")
            {
                DeviceId = "ST9730_001",
                IsConnected = true
                // Index는 읽기 전용이므로 설정할 수 없음
            };
            
            // 다이얼로그 표시
            using (var dialog = new TestConnectSuccessDialog(testDevice))
            {
                var result = dialog.ShowDialog(parentForm);
                
                if (result == DialogResult.OK)
                {
                    // 🎉 OK 버튼이 클릭된 경우 - 통합 데이터 구조체 사용
                    var testConnectData = dialog.GetTestConnectData();
                    var validationResult = testConnectData.Validate();
                    
                    // 📊 결과 표시
                    var resultMessage = $"✅ Test Connect Success Dialog 완료!\n\n" +
                                      $"📊 선택된 테스트: {testConnectData.GetSelectedTestCount()}/{testConnectData.TestItems.Count}개\n" +
                                      $"🗄️ DB 정보: {testConnectData.DatabaseInfo.IP}:{testConnectData.DatabaseInfo.Port} ({testConnectData.DatabaseInfo.ID})\n" +
                                      $"📱 APK 경로: {testConnectData.ApkInfo.FilePath}\n" +
                                      $"📶 Wi-Fi: {testConnectData.WirelessInfo.WifiSSID}, BLE: {testConnectData.WirelessInfo.BleName}\n" +
                                      $"검증 결과:\n{validationResult.GetSummary()}";
                    
                    MessageBox.Show(parentForm, resultMessage, "Dialog Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    // ❌ Cancel 버튼이 클릭된 경우
                    MessageBox.Show(parentForm, "❌ Test Connect 설정이 취소되었습니다.", "Dialog Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        /// <summary>
        /// 메인 폼에서 호출할 수 있는 메뉴 예제
        /// </summary>
        public static ToolStripMenuItem CreateTestMenuItem()
        {
            var menuItem = new ToolStripMenuItem("🎯 Test Connect Dialog");
            menuItem.Click += (s, e) =>
            {
                try
                {
                    ShowExample();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ 다이얼로그 표시 오류:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            return menuItem;
        }
    }
} 