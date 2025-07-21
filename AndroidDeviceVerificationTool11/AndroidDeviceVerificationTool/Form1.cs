using AndroidDeviceVerificationTool.Controls;
using AndroidDeviceVerificationTool.Data;
using MySql.Data.MySqlClient;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AndroidDeviceVerificationTool.Design.MaterialDesign;

namespace AndroidDeviceVerificationTool;

public partial class Form1 : Form
{
    private PeripheralTestControl peripheralTestControl;
    private ImeiSnWriteControl micInWriteControl;
    private PostSetupControl portSetupControl;
    
    // 🌐 통합 데이터 관리자 (모든 컨트롤이 공유)
    private SharedApplicationData sharedData;
    
    // 🔍 실시간 ADB 디바이스 모니터링
    private Data.AdbDeviceMonitor adbMonitor;
    
    // 🧪 Test Mode용 Communication 버튼들 (동적 생성)
    private Button btnPattern1, btnPattern2, btnPattern3, btnPattern4, btnTestAll;
    private AndroidDeviceVerificationTool.Examples.CommunicationExamples communicationExamples;
    


    private Data.UnifiedMqttManager unifiedMqtt;


    public static string GenerateNanoId(int length = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
                                    .Select(s => s[random.Next(s.Length)])
                                    .ToArray());
    }

    public Form1()
    {
        
        //Console.WriteLine("db_access_id: ");
        
        // 🚀 성능 최적화: 더블 버퍼링과 렌더링 최적화 활성화
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.UserPaint | 
                     ControlStyles.DoubleBuffer | 
                     ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
        
        // 🎨 레이아웃 일시 중단 (깜빡임 방지)
        this.SuspendLayout();

        try
        {
            this.BackColor = Color.FromArgb(47, 47, 55); //#2F2F37
            
            InitializeComponent();
            
            // 🌐 통합 데이터 관리자 초기화 (가장 먼저)
            sharedData = SharedApplicationData.Instance;
            
            // 🔍 ADB 디바이스 모니터 초기화 및 이벤트 구독
            adbMonitor = new Data.AdbDeviceMonitor();
            adbMonitor.DeviceConnected += OnAdbDeviceConnected;
            adbMonitor.DeviceDisconnected += OnAdbDeviceDisconnected;
            adbMonitor.LogMessage += OnAdbLogMessage;
            
            // 🔍 ADB 모니터링 시작 (백그라운드에서)
            _ = Task.Run(async () =>
            {
                try
                {
                    await adbMonitor.StartMonitoringAsync();
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        sharedData.AddDeviceLog(0, $"❌ ADB 모니터링 시작 실패: {ex.Message}", "ERROR");
                    }));
                }
            });
            
            // 🌐 통합 MQTT 관리자 초기화 및 실제 모드로 전환
            unifiedMqtt = UnifiedMqttManager.Instance;
            
            // 🔗 MQTT를 실제 브로커 모드로 전환 (백그라운드에서)
            _ = Task.Run(async () =>
            {
                try
                {
                    bool connected = await unifiedMqtt.SwitchToRealModeAsync();
                    if (connected)
                    {
                        this.Invoke(new Action(() =>
                        {
                            sharedData.AddDeviceLog(0, "MQTT Server connected.", "SUCCESS");
                            //Console.WriteLine()
                        }));
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            sharedData.AddDeviceLog(0, "MQTT Server Not Connected - Start Mock Mode", "WARNING");
                        }));
                    }
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        sharedData.AddDeviceLog(0, $"MQTT Initialize Failed: {ex.Message}", "ERROR");
                    }));
                }
            });
            
            // ⚙️ 메뉴바 생성 (빠르게)
            CreateMenuBar();
            
            // 🎯 컨트롤 초기화 (레이아웃 중단 상태에서)
            InitializeControlsOptimized();
        }
        finally
        {
            // 🎨 레이아웃 재개 (모든 변경사항을 한번에 적용)
            this.ResumeLayout(true);
        }
        
        // 🔍 데이터 로딩 (안정적으로 동기 실행)
        try
        {
            // 기본 디바이스 및 데이터 설정
            InitializeBasicDevicesOptimized();
            SetupAllTestFunctionsOptimized();  
            SetupExampleDataOptimized();
            
            // 🎯 모든 데이터 로딩 완료 후 UI 반영
            RefreshAllControlsUI();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"데이터 초기화 중 오류가 발생했습니다: {ex.Message}", "초기화 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        
        // 🔍 프로그램 시작시 마지막으로 사용한 설정 파일 자동 로드
        LoadLastUsedConfigurationFile();
        
        // FormClosed 이벤트 구독 (리소스 정리용)
        this.FormClosed += Form1_FormClosed;
    }

    // ⚙️ 메뉴바 생성
    private void CreateMenuBar()
    {
        // var menuStrip = new MenuStrip
        // {
        //     BackColor = Color.FromArgb(47, 47, 55),
        //     ForeColor = Color.White,
        //     Font = new Font("Noto Sans KR", 9F, FontStyle.Regular)
        // };   
        //
        // // Settings 메뉴
        // var settingsMenu = new ToolStripMenuItem
        // {
        //     Text = "Settings",
        //     ForeColor = Color.White
        // };
        //
        // // Test Connect Settings 메뉴 항목
        // var testConnectSettingsItem = new ToolStripMenuItem
        // {
        //     Text = "⚙️ Test Connect Settings",
        //     ForeColor = Color.White
        // };
        // testConnectSettingsItem.Click += TestConnectSettingsMenu_Click;
        //
        // settingsMenu.DropDownItems.Add(testConnectSettingsItem);
        // menuStrip.Items.Add(settingsMenu);
        //
        // this.MainMenuStrip = menuStrip;
        // this.Controls.Add(menuStrip);
    }

    private void OpenFileBrowser_Click(object sender, EventArgs e)
    {
        // 다이얼로그 형태의 파일 브라우저 사용
        string selectedFile = DarkFileBrowserDialog.ShowBrowserDialog(
            this, 
            "설정 파일 선택", 
            "JSON files (*.json)|*.json|All files (*.*)|*.*"
        );
        
        if (!string.IsNullOrEmpty(selectedFile))
        {
            // 파일이 선택된 경우
            var fileInfo = new FileInfo(selectedFile);
            sharedData.AddDeviceLog(0, $"📁 설정 파일 선택됨: {fileInfo.Name} ({FormatFileSize(fileInfo.Length)})", "INFO");
            sharedData.AddDeviceLog(0, $"📂 파일 경로: {selectedFile}", "INFO");

            // 🎯 실제 설정 파일 로드 및 데이터 적용
            LoadConfigurationFile(selectedFile);
        }
        else
        {
            // 취소된 경우
            sharedData.AddDeviceLog(0, "❌ 파일 선택이 취소되었습니다", "INFO");
        }
    }

    // 파일 크기 포맷팅 헬퍼 메서드 추가
    private string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int suffixIndex = 0;
        double size = bytes;

        while (size >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            size /= 1024;
            suffixIndex++;
        }
        return $"{size:F2} {suffixes[suffixIndex]}";
    }
    

    // ⚙️ Test Connect Settings 메뉴 클릭 이벤트
    private void TestConnectSettingsMenu_Click(object sender, EventArgs e)
    {
        // 연결된 디바이스 확인
        var connectedDevices = sharedData.Devices.Where(d => d.IsConnected).ToList();
        
        if (connectedDevices.Count == 0)
        {
            MessageBox.Show(this, "연결된 디바이스가 없습니다.\n디바이스를 먼저 연결해주세요.", 
                          "Test Connect Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        
        // 🎯 연결된 디바이스들로 TestConnectSuccessDialog 표시
        using (var dialog = new TestConnectSuccessDialog(connectedDevices))
        {
            var result = dialog.ShowDialog(this);
            
            if (result == DialogResult.OK)
            {
                sharedData.AddDeviceLog(0, $"✅ Test Connect 설정이 {connectedDevices.Count}개 디바이스에 저장되었습니다.", "SUCCESS");
            }
            else
            {
                sharedData.AddDeviceLog(0, "🔄 Test Connect 설정이 취소되었습니다.", "INFO");
            }
        }
    }



    // 🚀 최적화된 기본 디바이스 초기화 (UI 업데이트 최소화)
    private void InitializeBasicDevicesOptimized()
    {
        try
        {
            // 기본 4개 디바이스 슬롯 설정 (연결 상태는 모두 false)
            sharedData.SetupBasicDevices();
            
            // 🔍 AdbDeviceMonitor가 실시간으로 디바이스를 감지하므로
            // 초기화 시에는 강제 연결하지 않음
            
            // 기본 테스트 함수들은 디바이스가 실제 연결될 때 설정됨
            sharedData.AddDeviceLog(0, "🎯 기본 4개 슬롯 초기화 완료 (실제 디바이스 연결 대기 중)", "INFO");
        }
        catch (Exception ex)
        {
            sharedData?.AddDeviceLog(0, $"최적화된 디바이스 초기화 실패: {ex.Message}", "ERROR");
        }
    }

    // 🚀 최적화된 테스트 함수 설정 (배치 처리)
    private void SetupAllTestFunctionsOptimized()
    {
        try
        {
            // 🔍 실제 연결된 디바이스에만 테스트 함수들 등록
            var connectedDevices = sharedData.Devices.Where(d => d.IsConnected).ToList();
            
            if (connectedDevices.Count == 0)
            {
                sharedData.AddDeviceLog(0, "🎯 연결된 디바이스가 없음 - 테스트 함수 설정 건너뜀", "INFO");
                return;
            }
            
            foreach (var device in connectedDevices)
            {
                int deviceIndex = sharedData.GetIndexOfDevice(device);
                
                // Auto Test 함수들 (핵심만)
                sharedData.AddAutoTestFunction(deviceIndex, "Camera Test", "", () => TestCameraFunction(deviceIndex));
                sharedData.AddAutoTestFunction(deviceIndex, "WiFi Test", "", () => TestWiFiFunction(deviceIndex));
                sharedData.AddAutoTestFunction(deviceIndex, "Audio Test", "", () => TestAudioFunction(deviceIndex));
                sharedData.AddAutoTestFunction(deviceIndex, "Display Test", "", () => TestDisplayFunction(deviceIndex));
                sharedData.AddAutoTestFunction(deviceIndex, "Touch Test", "", () => TestTouchFunction(deviceIndex));
                
                // Manual Test 함수들 (핵심만)
                sharedData.AddManualTestFunction(deviceIndex, "Audio Test", "", () => TestAudioFunction(deviceIndex));
                sharedData.AddManualTestFunction(deviceIndex, "Display Test", "", () => TestDisplayFunction(deviceIndex));
                sharedData.AddManualTestFunction(deviceIndex, "Touch Test", "", () => TestTouchFunction(deviceIndex));
                
                sharedData.AddDeviceLog(deviceIndex, $"🧪 디바이스 {device.Name}에 테스트 함수들 설정 완료", "INFO");
            }
        }
        catch (Exception ex)
        {
            sharedData?.AddDeviceLog(0, $"최적화된 테스트 함수 설정 실패: {ex.Message}", "ERROR");
        }
    }

    // 🚀 최적화된 예시 데이터 설정 (필수만)
    private void SetupExampleDataOptimized()
    {
        try
        {
            // 연결된 디바이스에만 예시 데이터 설정
            var connectedDevices = sharedData.Devices.Where(d => d.IsConnected).ToList();
            
            foreach (var device in connectedDevices)
            {
                int deviceIndex = sharedData.GetIndexOfDevice(device);
                sharedData.Set_IMEI_SN_Data(deviceIndex, 
                    $"IMEI_{deviceIndex:D3}_{DateTime.Now:HHmm}", 
                    $"SN_{deviceIndex:D4}_{DateTime.Now:MMdd}", 
                    $"Model_{device.Name}");
            }
        }
        catch (Exception ex)
        {
            sharedData?.AddDeviceLog(0, $"최적화된 예시 데이터 설정 실패: {ex.Message}", "ERROR");
        }
    }

    // 기존 메서드들 (호환성 유지)
    private void InitializeBasicDevices()
    {
        InitializeBasicDevicesOptimized();
    }

    private void SetupAllTestFunctions()
    {
        SetupAllTestFunctionsOptimized();
    }

    private void SetupExampleData()
    {
        SetupExampleDataOptimized();
    }

    // 🧪 모든 디바이스에 테스트 함수들 설정 (기존 버전 - 호환성용)
    private void SetupAllTestFunctionsLegacy()
    {
        // 4개 디바이스에 테스트 함수들 등록 (0-3 인덱스)
        for (int i = 0; i < 4; i++)
        {
            int capture = i;
            // Auto Test 함수들 등록 - string 반환 함수로 변경
            sharedData.AddAutoTestFunction(i, "Camera Test", "", () => TestCameraFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            sharedData.AddAutoTestFunction(i, "WiFi Test", "", () => TestWiFiFunction(capture));
            
            // Manual Test 함수들 등록 - string 반환 함수로 변경
            sharedData.AddManualTestFunction(i, "Audio Test", "", () => TestAudioFunction(capture));
            sharedData.AddManualTestFunction(i, "Display Test", "", () => TestDisplayFunction(capture));
            sharedData.AddManualTestFunction(i, "Touch Test", "", () => TestTouchFunction(capture));
            sharedData.AddManualTestFunction(i, "Touch Test", "", () => TestTouchFunction(capture));
            sharedData.AddManualTestFunction(i, "Touch Test", "", () => TestTouchFunction(capture));
            sharedData.AddManualTestFunction(i, "Touch Test", "", () => TestTouchFunction(capture));
            sharedData.AddManualTestFunction(i, "Touch Test", "", () => TestTouchFunction(capture));
            sharedData.AddManualTestFunction(i, "Touch Test", "", () => TestTouchFunction(capture));
            sharedData.AddManualTestFunction(i, "Touch Test", "", () => TestTouchFunction(capture));
            
            sharedData.AddDeviceLog(i, $"🧪 테스트 함수들이 설정되었습니다", "INFO");
        }
    }



    private void Form1_FormClosed(object sender, FormClosedEventArgs e)
    {
        // ADB 모니터링 중지
        try
        {
            _ = Task.Run(async () => await adbMonitor?.StopMonitoringAsync());
        }
        catch (Exception ex)
        {
            sharedData?.AddDeviceLog(0, $"ADB 모니터링 중지 오류: {ex.Message}", "ERROR");
        }
    }
    
    // 🔍 ADB 디바이스 연결 이벤트 핸들러
    private void OnAdbDeviceConnected(Data.AdbDevice device)
    {
        try
        {
            this.Invoke(new Action(() =>
            {
                sharedData.AddDeviceLog(0, $"🔗 디바이스 자동 감지: {device.DeviceId} ({device.Status})", "SUCCESS");
                
                // 빈 슬롯 자동 할당
                int selectedSlot = FindNextAvailableSlot();
                
                if (selectedSlot == -1)
                {
                    sharedData.AddDeviceLog(0, $"❌ 모든 슬롯이 사용 중입니다. 연결을 거부합니다: {device.DeviceId}", "ERROR");
                    sharedData.AddDeviceLog(0, "💡 기존 디바이스를 연결 해제한 후 다시 시도하세요.", "INFO");
                    return;
                }
                
                // 자동 설정 처리
                AutoSetupConnectedDevice(device.DeviceId, selectedSlot);
            }));
        }
        catch (Exception ex)
        {
            sharedData?.AddDeviceLog(0, $"디바이스 연결 처리 오류: {ex.Message}", "ERROR");
        }
    }
    
    // 🔌 ADB 디바이스 연결 해제 이벤트 핸들러
    private void OnAdbDeviceDisconnected(string deviceId)
    {
        try
        {
            this.Invoke(new Action(() =>
            {
                sharedData.AddDeviceLog(0, $"🔌 디바이스 연결 해제 감지: {deviceId}", "WARNING");
                
                // 해당 디바이스를 찾아서 연결 해제 처리
                for (int i = 0; i < sharedData.Devices.Count; i++)
                {
                    var device = sharedData.Devices[i];
                    if (device.IsConnected && device.DeviceId == deviceId)
                    {
                        sharedData.DisconnectDevice(i);
                        sharedData.AddDeviceLog(i, $"✅ 슬롯 {i + 1} 자동 연결 해제: {deviceId}", "INFO");
                        
                        // UI 갱신
                        RefreshAllControlsUI();
                        break;
                    }
                }
            }));
        }
        catch (Exception ex)
        {
            sharedData?.AddDeviceLog(0, $"디바이스 연결 해제 처리 오류: {ex.Message}", "ERROR");
        }
    }
    
    // 📝 ADB 로그 메시지 핸들러
    private void OnAdbLogMessage(string message)
    {
        try
        {
            this.Invoke(new Action(() =>
            {
                //sharedData.AddDeviceLog(0, $"[ADB] {message}", "DEBUG");
                Console.WriteLine($"[ADB] {message}");
            }));
        }
        catch
        {
            // UI 스레드가 사용 불가능할 수 있으므로 무시
        }
    }
    
    /// <summary>
    /// 🎯 연결된 디바이스 자동 설정 처리
    /// </summary>
    private void AutoSetupConnectedDevice(string deviceId, int slotIndex)
    {
        try
        {
            int dev_sub_port = unifiedMqtt.GetDevicePort(slotIndex);
            string pubTopic = unifiedMqtt.GetDevicePubTopic(slotIndex);
            string subTopic = unifiedMqtt.GetDeviceSubTopic(slotIndex);

            sharedData.AddDeviceLog(slotIndex, $"🎯 디바이스 자동 설정 시작: {deviceId} → 슬롯 {slotIndex + 1}, Topic: {pubTopic}, Port: {dev_sub_port}", "INFO");
            
            // **먼저 UI 설정을 즉시 완료** (인덱스 꼬임 방지)
            // 디바이스 슬롯에 배치
            sharedData.AddDeviceLog(slotIndex, $"AssignDeviceToSlot 실행: {deviceId} → 슬롯 {slotIndex}", "DEBUG");
            AssignDeviceToSlot(deviceId, slotIndex);
            sharedData.AddDeviceLog(slotIndex, $"AssignDeviceToSlot 완료", "DEBUG");
            
            // 🔄 저장된 JSON 설정이 있으면 자동 적용, 없으면 기본 설정
            sharedData.AddDeviceLog(slotIndex, $"JSON 설정 확인 중... HasActiveJsonConfig: {sharedData.HasActiveJsonConfig}", "DEBUG");
            
            //JSON파일 로딩해서 처리함. 
            bool jsonApplied = sharedData.ApplyCurrentJsonConfigToDevice(slotIndex);
            sharedData.AddDeviceLog(slotIndex, $"JSON 적용 결과: {jsonApplied}", "DEBUG");

/*            
            //if (!jsonApplied)
            if (false)
            {
                // JSON 설정이 없는 경우에만 기본 테스트 함수와 예시 데이터 설정
                sharedData.AddDeviceLog(slotIndex, "📋 JSON 설정이 없어 기본 설정을 적용합니다", "INFO");
                
                // 🧪 기본 테스트 함수들 추가
                sharedData.AddDeviceLog(slotIndex, "🔧 SetupTestFunctionsForDevice 호출 시작", "DEBUG");
                SetupTestFunctionsForDevice(slotIndex);
                sharedData.AddDeviceLog(slotIndex, "🔧 SetupTestFunctionsForDevice 호출 완료", "DEBUG");
                
                // 🔧 기본 예시 데이터 설정
                sharedData.Set_IMEI_SN_Data(slotIndex, 
                    $"IMEI_{slotIndex:D3}_{DateTime.Now:HHmm}", 
                    $"SN_{slotIndex:D4}_{DateTime.Now:MMdd}", 
                    deviceId);
                sharedData.SetApnData(slotIndex, "KT", "KT", "KT");
                
                sharedData.AddDeviceLog(slotIndex, "✅ 기본 설정 완료", "INFO");
            }
            else
            {
                sharedData.AddDeviceLog(slotIndex, "✅ JSON 설정 적용 완료", "INFO");
            }
  */

            // 🔥 설정 완료 후 강제 UI 갱신 (첫 번째 디바이스 문제 해결용)
            //sharedData.AddDeviceLog(slotIndex, "🔥 설정 완료 후 강제 UI 갱신 실행", "DEBUG");
            sharedData.TriggerDeviceDataChanged(slotIndex);
            RefreshAllControlsUI();
            
            
            sharedData.AddDeviceLog(slotIndex, $"✅ 슬롯 {slotIndex + 1} UI 설정 완료! 백그라운드 작업 시작...", "SUCCESS");
            
            // 🔧 **그 다음 백그라운드에서 ADB 작업들 수행** (UI 설정과 분리)
            _ = Task.Run(async () =>
            {
                try
                {
                    sharedData.AddDeviceLog(slotIndex, $"Start ADB task. devId:{deviceId}", "INFO");
                    Console.WriteLine($"slotIndex:{slotIndex}, Start ADB task. deviceId:{deviceId}");

                    //삭제 : 삭제는 테스트 후 Save Result할때 하면 됨
                    //install_apk(deviceId, false);


                    open_port_forwarding(deviceId, dev_sub_port.ToString());
                    bool apk_installed = false;
                    if (is_apk_installed(deviceId))
                    {
                        Console.WriteLine("APK file installed already.");
                        apk_installed = true;
                    }
                    else
                        install_apk(deviceId, true);

                    //if (apk_installed)
                    //{
                    //    var param_data = new Dictionary<string, object>
                    //    {
                    //        ["topic_dev_sub"] = pubTopic,
                    //        ["topic_dev_pub"] = subTopic    //Android dev의 pub topic 은 PC의 sub topic 
                    //    };

                    //    await Task.Delay(10000);
                    //    var response = unifiedMqtt.send_to_device(slotIndex, null, "dev_join_req", param_data);
                    //    //Console.WriteLine($"Response: {response}");

                    //}

                    if (apk_installed)
                    {
                        var param_data = new Dictionary<string, object>
                        {
                            ["topic_dev_sub"] = pubTopic,
                            ["topic_dev_pub"] = subTopic     //Android dev의 pub topic 은 PC의 sub topic 
                        };

                        MqttRes res = null;
                        bool toggle_flag = false;
                        while (true)
                        {
                            Console.WriteLine($"[Loop] Sending dev_join_req to {deviceId}...");
                            toggle_flag = !toggle_flag;
                            string pubT = "";
                            if (toggle_flag == true) pubT = deviceId;
                            else pubT = null;

                            //Console.WriteLine($"===================pubT:{pubT}, toggle_flag:{toggle_flag}");
                            //if(pubT == null)
                            //{
                            //    Console.WriteLine("!!!!!!!!!!NULL");
                            //}

                            //string pubT = null;

                            //await Task.Delay(5000);
                            res = await unifiedMqtt.send_to_device(slotIndex, pubT, "dev_join_req", param_data);

                            if (res != null)
                            {
                                if(res.Result == true)
                                {
                                    Console.WriteLine("send_to_device/ exit loop.");
                                    break;
                                }
                            }
                            else
                            {
                                Console.WriteLine($"send_to_device/ waiting... No valid response yet....");
                            }

                            //await Task.Delay(1000); // 1초 대기
                        }


                    }

                    // 완료 로그는 UI 스레드에서
                    this.Invoke(new Action(() =>
                    {
                        unifiedMqtt.PrintDeviceMappings();
                        sharedData.AddDeviceLog(slotIndex, $"🎉 디바이스 {deviceId} 전체 설정 완료!", "SUCCESS");
                    }));
                }
                catch (Exception ex)
                {
                    this.Invoke(new Action(() =>
                    {
                        sharedData.AddDeviceLog(slotIndex, $"❌ 백그라운드 ADB 작업 실패: {ex.Message}", "ERROR");
                        sharedData.AddDeviceLog(slotIndex, "💡 UI 설정은 완료되었으며, 수동으로 ADB 작업을 진행할 수 있습니다", "INFO");
                    }));
                }
            });
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(slotIndex, $"❌ 디바이스 자동 설정 오류: {ex.Message}", "ERROR");
        }
    }
    
    /// <summary>
    /// 🧪 특정 디바이스에 테스트 함수들 설정
    /// </summary>
    private void SetupTestFunctionsForDevice(int deviceIndex)
    {
        try
        {
            sharedData.AddDeviceLog(deviceIndex, $"🧪 SetupTestFunctionsForDevice 시작 - 디바이스 인덱스: {deviceIndex}", "DEBUG");
            
            // 디바이스 존재 확인
            var device = sharedData.GetDeviceByIndex(deviceIndex);
            if (device == null)
            {
                sharedData.AddDeviceLog(deviceIndex, $"❌ 디바이스를 찾을 수 없음: 인덱스 {deviceIndex}", "ERROR");
                return;
            }
            
            sharedData.AddDeviceLog(deviceIndex, $"📋 디바이스 확인됨: {device.Name}, 연결상태: {device.IsConnected}", "DEBUG");
            
            // Auto Test 함수들 추가 전 로그
            sharedData.AddDeviceLog(deviceIndex, "🤖 Auto Test 함수들 추가 시작", "DEBUG");
            sharedData.AddAutoTestFunction(deviceIndex, "Camera Test", "", () => TestCameraFunction(deviceIndex));
            sharedData.AddAutoTestFunction(deviceIndex, "WiFi Test", "", () => TestWiFiFunction(deviceIndex));
            sharedData.AddAutoTestFunction(deviceIndex, "Audio Test", "", () => TestAudioFunction(deviceIndex));
            sharedData.AddAutoTestFunction(deviceIndex, "Display Test", "", () => TestDisplayFunction(deviceIndex));
            sharedData.AddAutoTestFunction(deviceIndex, "Touch Test", "", () => TestTouchFunction(deviceIndex));
            sharedData.AddDeviceLog(deviceIndex, "✅ Auto Test 함수들 추가 완료", "DEBUG");
            
            // Manual Test 함수들 추가 전 로그
            sharedData.AddDeviceLog(deviceIndex, "🔧 Manual Test 함수들 추가 시작", "DEBUG");
            sharedData.AddManualTestFunction(deviceIndex, "Audio Test", "", () => TestAudioFunction(deviceIndex));
            sharedData.AddManualTestFunction(deviceIndex, "Display Test", "", () => TestDisplayFunction(deviceIndex));
            sharedData.AddManualTestFunction(deviceIndex, "Touch Test", "", () => TestTouchFunction(deviceIndex));
            sharedData.AddDeviceLog(deviceIndex, "✅ Manual Test 함수들 추가 완료", "DEBUG");
            
            // 최종 카운트 확인
            var autoCount = device.PeripheralData.AutoTests.Count;
            var manualCount = device.PeripheralData.ManualTests.Count;
            sharedData.AddDeviceLog(deviceIndex, $"📊 최종 테스트 카운트 - Auto: {autoCount}, Manual: {manualCount}", "INFO");
            
            // 🔥 강제로 DeviceDataChanged 이벤트 발생 (UI 갱신)
            sharedData.AddDeviceLog(deviceIndex, "🔥 강제 UI 갱신 이벤트 발생", "DEBUG");
            sharedData.TriggerDeviceDataChanged(deviceIndex);
            
            // 🔥 추가로 전체 UI 새로고침도 실행
            sharedData.AddDeviceLog(deviceIndex, "🔥 전체 UI 새로고침 실행", "DEBUG");
            RefreshAllControlsUI();
            
            sharedData.AddDeviceLog(deviceIndex, $"🧪 슬롯 {deviceIndex + 1} 테스트 함수 설정 완료", "INFO");
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(deviceIndex, $"❌ 테스트 함수 설정 실패: {ex.Message}", "ERROR");
            sharedData.AddDeviceLog(deviceIndex, $"❌ 스택 트레이스: {ex.StackTrace}", "ERROR");
        }
    }

    // 🚀 최적화된 컨트롤 초기화 (깜빡임 최소화)
    private void InitializeControlsOptimized()
    {
        // 각 패널의 레이아웃도 일시 중단
        peripheralTestPanel.SuspendLayout();
        micInWritePanel.SuspendLayout();
        postSetupPanel.SuspendLayout();

        try
        {
            // UserControl 인스턴스 생성 (통합 데이터 시스템 사용)
            peripheralTestControl = new PeripheralTestControl();
            micInWriteControl = new ImeiSnWriteControl();
            portSetupControl = new PostSetupControl();

            // Dock 설정을 먼저 하고 패널에 추가 (렌더링 최적화)
            peripheralTestControl.Dock = DockStyle.Fill;
            micInWriteControl.Dock = DockStyle.Fill;
            portSetupControl.Dock = DockStyle.Fill;

            // 커스텀 탭 패널에 추가
            peripheralTestPanel.Controls.Add(peripheralTestControl);
            micInWritePanel.Controls.Add(micInWriteControl);
            postSetupPanel.Controls.Add(portSetupControl);

            // 🔍 **임시 테스트용 라벨 추가**
            var testLabel2 = new Label
            {
                Text = "IMEI/SN Write Control Test",
                Location = new Point(50, 50),
                Size = new Size(300, 50),
                BackColor = Color.Red,
                ForeColor = Color.White,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            micInWritePanel.Controls.Add(testLabel2);

            var testLabel3 = new Label
            {
                Text = "Post Setup Control Test", 
                Location = new Point(50, 50),
                Size = new Size(300, 50),
                BackColor = Color.Blue,
                ForeColor = Color.White,
                Font = new Font("Arial", 14, FontStyle.Bold)
            };
            postSetupPanel.Controls.Add(testLabel3);

            // 🔍 **디버그: 컨트롤 생성 및 추가 확인**
            sharedData.AddDeviceLog(0, $"✅ PeripheralTestControl 생성됨: {peripheralTestControl != null}", "DEBUG");
            sharedData.AddDeviceLog(0, $"✅ MicInWriteControl 생성됨: {micInWriteControl != null}", "DEBUG");  
            sharedData.AddDeviceLog(0, $"✅ PostSetupControl 생성됨: {portSetupControl != null}", "DEBUG");
            sharedData.AddDeviceLog(0, $"✅ Panel 1 컨트롤 수: {peripheralTestPanel.Controls.Count}", "DEBUG");
            sharedData.AddDeviceLog(0, $"✅ Panel 2 컨트롤 수: {micInWritePanel.Controls.Count}", "DEBUG");
            sharedData.AddDeviceLog(0, $"✅ Panel 3 컨트롤 수: {postSetupPanel.Controls.Count}", "DEBUG");

            // 🎯 이벤트 구독 추가
            SubscribeToControlEvents();
            
            // 🏷️ 초기 모델명 설정
            if (modelNameTextBox != null)
            {
                modelNameTextBox.Text = sharedData.ModelName;
            }
        }
        finally
        {
            // 각 패널의 레이아웃 재개
            peripheralTestPanel.ResumeLayout(true);
            micInWritePanel.ResumeLayout(true);
            postSetupPanel.ResumeLayout(true);
        }
    }

    // 🔍 백그라운드에서 데이터 초기화 (UI 스레드 블로킹 방지)
    private void InitializeDataInBackground()
    {
        Task.Run(async () =>
        {
            try
            {
                // 짧은 대기로 UI 렌더링 완료 보장
                await Task.Delay(50);
                
                // UI 스레드에서 기본 디바이스 설정
                this.Invoke(new Action(() =>
                {
                    // 레이아웃 일시 중단으로 깜빡임 방지
                    this.SuspendLayout();
                    try
                    {
                        InitializeBasicDevicesOptimized();
                    }
                    finally
                    {
                        this.ResumeLayout(false); // 강제 리페인트 하지 않음
                    }
                }));

                // 추가 대기 후 테스트 함수들 설정
                await Task.Delay(100);
                this.Invoke(new Action(() =>
                {
                    SetupAllTestFunctionsOptimized();
                    SetupExampleDataOptimized();
                }));
            }
            catch (Exception ex)
            {
                // 오류 시에만 첫 번째 디바이스에 로그
                this.Invoke(new Action(() =>
                {
                    sharedData?.AddDeviceLog(0, $"백그라운드 초기화 실패: {ex.Message}", "ERROR");
                }));
            }
        });
    }

    // 🎯 모든 컨트롤 UI 새로고침 (데이터 로딩 완료 후)
    private void RefreshAllControlsUI()
    {
        try
        {
            sharedData?.AddDeviceLog(0, "🔄 모든 컨트롤 UI 새로고침 시작", "DEBUG");
            
            // 각 컨트롤의 RefreshUI 호출 (개별 try-catch로 안전하게)
            try
            {
                peripheralTestControl?.RefreshUI();
                sharedData?.AddDeviceLog(0, "✅ PeripheralTestControl RefreshUI 완료", "DEBUG");
            }
            catch (Exception ex)
            {
                sharedData?.AddDeviceLog(0, $"❌ PeripheralTestControl RefreshUI 실패: {ex.Message}", "ERROR");
            }
            
            try
            {
                micInWriteControl?.RefreshUI();
                sharedData?.AddDeviceLog(0, "✅ MicInWriteControl RefreshUI 완료", "DEBUG");
            }
            catch (Exception ex)
            {
                sharedData?.AddDeviceLog(0, $"❌ MicInWriteControl RefreshUI 실패: {ex.Message}", "ERROR");
            }
            
            try
            {
                portSetupControl?.RefreshUI();
                sharedData?.AddDeviceLog(0, "✅ PostSetupControl RefreshUI 완료", "DEBUG");
            }
            catch (Exception ex)
            {
                sharedData?.AddDeviceLog(0, $"❌ PostSetupControl RefreshUI 실패: {ex.Message}", "ERROR");
            }
            
            sharedData?.AddDeviceLog(0, "✅ 모든 컨트롤 UI 새로고침 완료", "SUCCESS");
        }
        catch (Exception ex)
        {
            sharedData?.AddDeviceLog(0, $"❌ UI 새로고침 실패: {ex.Message}", "ERROR");
        }
    }

    // 기존 InitializeControls 메서드 (호환성 유지)
    private void InitializeControls()
    {
        InitializeControlsOptimized();
    }

    // 🎯 컨트롤 이벤트 구독
    private void SubscribeToControlEvents()
    {
        // 🌐 SharedApplicationData 이벤트 구독 (JSON 로드 시 UI 갱신)
        sharedData.DeviceDataChanged += OnSharedDeviceDataChanged;
        sharedData.DeviceConnectionChanged += OnSharedDeviceConnectionChanged;
        sharedData.DeviceAdded += OnSharedDeviceAdded;
        sharedData.DeviceRemoved += OnSharedDeviceRemoved;
        sharedData.ModelNameChanged += OnModelNameChanged;

        // 2번 탭 (MicInWriteControl) 이벤트 구독
        micInWriteControl.WriteButtonClicked += OnMicInWriteButtonClicked;

        // 3번 탭 (PostSetupControl) 이벤트 구독
        portSetupControl.WriteButtonClicked += OnPostSetupWriteButtonClicked;
        portSetupControl.SaveResultButtonClicked += OnPostSetupSaveResultButtonClicked;

        sharedData.AddDeviceLog(0, "🎯 Form1: 모든 이벤트가 구독되었습니다 (SharedData + 컨트롤)", "INFO");
    }

    // 🔄 SharedApplicationData 이벤트 핸들러들
    private void OnSharedDeviceDataChanged(int deviceIndex, UnifiedDeviceData device)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnSharedDeviceDataChanged(deviceIndex, device)));
            return;
        }

        try
        {
            // 📄 JSON 로드 등 주요 데이터 변경 시에만 전체 갱신 (선택적)
            sharedData.AddDeviceLog(deviceIndex, $"🔄 Form1: 디바이스 데이터 변경 감지 - Device #{deviceIndex} (부분 갱신)", "INFO");
            
            // 🎯 각 컨트롤이 자체적으로 SharedData 이벤트를 구독하므로 Form1에서는 추가 처리 안함
            // RefreshAllControlsUI(); // 제거: 각 컨트롤에서 알아서 처리함
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(deviceIndex, $"❌ Form1: 데이터 변경 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    private void OnSharedDeviceConnectionChanged(int deviceIndex, UnifiedDeviceData device)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnSharedDeviceConnectionChanged(deviceIndex, device)));
            return;
        }

        try
        {
            sharedData.AddDeviceLog(deviceIndex, $"📱 Form1: 디바이스 연결 상태 변경 - Device #{deviceIndex} ({device.Name})", "INFO");
            // 연결 상태 변경은 각 컨트롤에서 처리하므로 별도 작업 불필요
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(deviceIndex, $"❌ Form1: 연결 상태 변경 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    private void OnSharedDeviceAdded(UnifiedDeviceData device)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnSharedDeviceAdded(device)));
            return;
        }

        try
        {
            int deviceIndex = sharedData.GetIndexOfDevice(device);
            sharedData.AddDeviceLog(deviceIndex, $"➕ Form1: 새 디바이스 추가됨 - {device.Name}", "INFO");
            // RefreshAllControlsUI(); // 제거: 각 컨트롤에서 DeviceAdded 이벤트로 처리함
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(0, $"❌ Form1: 디바이스 추가 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    private void OnSharedDeviceRemoved(int deviceIndex)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnSharedDeviceRemoved(deviceIndex)));
            return;
        }

        try
        {
            sharedData.AddDeviceLog(deviceIndex, $"➖ Form1: 디바이스 제거됨 - Index #{deviceIndex}", "INFO");
            RefreshAllControlsUI();
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(deviceIndex, $"❌ Form1: 디바이스 제거 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    private void OnModelNameChanged(string modelName)
    {
        if (InvokeRequired)
        {
            Invoke(new Action(() => OnModelNameChanged(modelName)));
            return;
        }

        try
        {
            // 타이틀 바의 TextBox 업데이트
            if (modelNameTextBox != null)
            {
                modelNameTextBox.Text = modelName;
            }
            sharedData.AddDeviceLog(0, $"🏷️ Form1: 모델명 UI 업데이트됨 - '{modelName}'", "INFO");
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(0, $"❌ Form1: 모델명 변경 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    // 🎯 2번 탭 Write 버튼 클릭 이벤트 핸들러
    private void OnMicInWriteButtonClicked(UnifiedDeviceData device)
    {
        try
        {
            int deviceIndex = sharedData.GetIndexOfDevice(device);
            sharedData.AddDeviceLog(deviceIndex, $"📝 Form1: MicInWrite Write 버튼 클릭됨 - Device #{deviceIndex} ({device.Name})", "INFO");
            
            // 실제 IMEI/SN 쓰기 로직을 여기에 구현
            ProcessMicInWrite(device);
        }
        catch (Exception ex)
        {
            int deviceIndex = sharedData.GetIndexOfDevice(device);
            sharedData.AddDeviceLog(deviceIndex, $"❌ MicInWrite 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    // 🎯 3번 탭 Write 버튼 클릭 이벤트 핸들러
    private void OnPostSetupWriteButtonClicked(UnifiedDeviceData device)
    {
        try
        {
            int deviceIndex = sharedData.GetIndexOfDevice(device);
            sharedData.AddDeviceLog(deviceIndex, $"📮 Form1: PostSetup Write 버튼 클릭됨 - Device #{deviceIndex} ({device.Name})", "INFO");
            
            // 실제 APN 쓰기 로직을 여기에 구현
            ProcessPostSetupWrite(device);
        }
        catch (Exception ex)
        {
            int deviceIndex = sharedData.GetIndexOfDevice(device);
            sharedData.AddDeviceLog(deviceIndex, $"❌ PostSetup Write 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    // 🎯 3번 탭 Save Result 버튼 클릭 이벤트 핸들러
    private void OnPostSetupSaveResultButtonClicked(UnifiedDeviceData device)
    {
        try
        {
            int deviceIndex = sharedData.GetIndexOfDevice(device);
            sharedData.AddDeviceLog(deviceIndex, $"📮 Form1: PostSetup Save Result 버튼 클릭됨 - Device #{deviceIndex} ({device.Name})", "INFO");
            
            // 실제 결과 저장 로직을 여기에 구현
            ProcessSaveResult(device);
        }
        catch (Exception ex)
        {
            int deviceIndex = sharedData.GetIndexOfDevice(device);
            sharedData.AddDeviceLog(deviceIndex, $"❌ Save Result 처리 중 오류: {ex.Message}", "ERROR");
        }
    }

    // 🎯 실제 MicInWrite 처리 로직
    private void ProcessMicInWrite(UnifiedDeviceData device)
    {
        // IMEI/SN 쓰기 실제 구현
        var micInData = device.IMEI_SN_Data;
        int deviceIndex = sharedData.GetIndexOfDevice(device);
        
        sharedData.AddDeviceLog(deviceIndex, $"📝 IMEI 쓰기 시작: {micInData.IMEI}", "INFO");
        sharedData.AddDeviceLog(deviceIndex, $"📝 Serial Number 쓰기 시작: {micInData.SerialNumber}", "INFO");
        
        // 여기에 실제 디바이스에 IMEI/SN을 쓰는 로직 구현
        // 예: ADB 명령어 실행, FastBoot 명령어 등
        
        sharedData.AddDeviceLog(deviceIndex, $"✅ MicInWrite 완료: Device #{deviceIndex}", "SUCCESS");
    }

    // 🎯 실제 PostSetup Write 처리 로직
    private void ProcessPostSetupWrite(UnifiedDeviceData device)
    {
        // APN 설정 쓰기 실제 구현
        int deviceIndex = sharedData.GetIndexOfDevice(device);
        sharedData.AddDeviceLog(deviceIndex, $"📮 APN 설정 쓰기 시작", "INFO");
        
        // 여기에 실제 디바이스에 APN 설정을 쓰는 로직 구현
        // 예: ADB 명령어로 설정 변경, API 호출 등
        
        sharedData.AddDeviceLog(deviceIndex, $"✅ PostSetup Write 완료: Device #{deviceIndex}", "SUCCESS");
    }

    // 🎯 실제 Save Result 처리 로직
    private void ProcessSaveResult(UnifiedDeviceData device)
    {
        // 결과 저장 실제 구현
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        int deviceIndex = sharedData.GetIndexOfDevice(device);
        
        sharedData.AddDeviceLog(deviceIndex, $"💾 결과 저장 시작: {timestamp}", "INFO");
        
        // 여기에 실제 결과를 파일이나 데이터베이스에 저장하는 로직 구현
        // 예: CSV 파일 저장, 데이터베이스 INSERT 등
        
        sharedData.AddDeviceLog(deviceIndex, $"✅ Save Result 완료: Device #{deviceIndex}", "SUCCESS");
    }

    private void SetupDeviceConnectionSimulation()
    {
        // 디바이스 목록 확인
        sharedData.AddDeviceLog(0, $"🔍 연결 시뮬레이션 시작 - 현재 디바이스 수: {sharedData.Devices.Count}", "DEBUG");
        for (int i = 0; i < sharedData.Devices.Count; i++)
        {
            var device = sharedData.Devices[i];
            sharedData.AddDeviceLog(i, $"🔍 디바이스 확인 - Index={i}, Name={device.Name}, Connected={device.IsConnected}", "DEBUG");
        }

        // 1초 후 첫 번째 디바이스 연결 시뮬레이션
        Task.Delay(1000).ContinueWith(t =>
        {
            this.Invoke(new Action(() =>
            {
                sharedData.AddDeviceLog(0, "🚀 VD #01 연결 시도 시작...", "INFO");
                sharedData.ConnectDevice(0, "GALAXY_S23_001");
                sharedData.AddDeviceLog(0, "🔗 자동 연결 시뮬레이션: Device #1", "SUCCESS");
                
                // 연결 후 상태 확인
                if (sharedData.Devices.Count > 0)
                {
                    var device = sharedData.Devices[0];
                    sharedData.AddDeviceLog(0, $"📊 연결 후 VD #01 상태: Connected={device.IsConnected}, DeviceId={device.DeviceId}", "DEBUG");
                }
            }));
        });

        // 🚀 추가 디바이스들 연결 시뮬레이션 (3초 후)
        Task.Delay(3000).ContinueWith(t =>
        {
            this.Invoke(new Action(() =>
            {
                // 두 번째 디바이스 연결
                sharedData.ConnectDevice(1, "IPHONE_15_002");
                sharedData.AddDeviceLog(1, "🚀 IPHONE_15_002 디바이스가 연결되었습니다", "SUCCESS");
                
                // 세 번째 디바이스 연결 (5초 후)
                Task.Delay(2000).ContinueWith(t2 =>
                {
                    this.Invoke(new Action(() =>
                    {
                        sharedData.ConnectDevice(2, "PIXEL_8_003");
                        sharedData.AddDeviceLog(2, "🚀 PIXEL_8_003 디바이스가 연결되었습니다", "SUCCESS");
                    }));
                });
            }));
        });

        // ✅ 자동 테스트 상태 변경 시뮬레이션 제거 - 사용자가 직접 Start 버튼을 눌러야 함
        // 이제 테스트는 오직 Start 버튼을 통해서만 실행됩니다
    }

    // TabControl 관련 메서드들 제거됨 - 커스텀 탭 구조 사용

    // 🎯 Test Connection 버튼 기능 (JSON 로드 테스트 추가)
    private void TestConnectionButton_Click(object sender, EventArgs e) 
    {
        try
        {
            // 현재 디바이스 수 확인
            var deviceCount = sharedData.Devices.Count;
            
            sharedData.AddDeviceLog(0, "🚀 Test Connection 버튼 클릭 - JSON 로드 테스트 시작", "INFO");
            
            // 1. 기존 디바이스 연결 시뮬레이션
            // SetupDeviceConnectionSimulation();
            
            // 2. JSON 로드 테스트 추가
            TestJsonLoad();
            
            sharedData.AddDeviceLog(0, $"🔄 연결 테스트 및 JSON 로드 완료 (디바이스 수: {deviceCount})", "SUCCESS");
            
            // 🧪 Test Mode로 전환 - Communication 버튼들 표시
            ShowTestModeButtons();
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(0, $"테스트 실행 실패: {ex.Message}", "ERROR");
        }
    }


    private bool is_apk_installed(string dev_id)
    {
        try
        {
            string filepath = "/system/etc/permissions/privapp-permissions-com.example.vtandapp.xml";
            var process = new Process();
            process.StartInfo.FileName = "adb";
            process.StartInfo.Arguments = $"-s {dev_id} shell ls {filepath}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            string output = process.StandardOutput.ReadToEnd().Trim();
            string error = process.StandardError.ReadToEnd().Trim();

            process.WaitForExit();

            //Console.WriteLine($"[is_apk_installed] output: {output}");
            //Console.WriteLine($"[is_apk_installed] error: {error}");

                 // 성공적으로 파일 경로가 출력되었는지 확인
            if (!string.IsNullOrEmpty(output) && output.Contains("privapp-permissions-com.example.vtandapp.xml") && string.IsNullOrEmpty(error))
                return true;

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[is_apk_installed] Exception: {ex.Message}");
            return false;
        }
    }

    private void open_port_forwarding(string dev_id, string dev_sub_port)
    {
        sharedData.AddDeviceLog(0, $"openPF/{dev_id},{dev_sub_port}", "DEBUG");

        var adbCommands = new List<string>
        {
            "adb -s "+dev_id+" forward tcp:"+dev_sub_port+" tcp:1883",
            "adb -s "+dev_id+" reverse tcp:1883 tcp:1883"
        };

        foreach (var adbCommand in adbCommands)
        {
            Console.WriteLine($"[실행 중] {adbCommand}");

            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + adbCommand)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Console.WriteLine("=== STDOUT ===");
                    Console.WriteLine(output);

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        Console.WriteLine("=== STDERR ===");
                        Console.WriteLine(error);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("실행 중 오류 발생: " + ex.Message);
            }
            //Thread.Sleep(2000);

            //Console.WriteLine();
        }
    }

    private void install_apk(string dev_id, bool install_flag)
    {
        var apk_file = Config.Instance.GetNested("target_apk_file") as string;
        var permission_file_path = Config.Instance.GetNested("privapp_permission_file") as string;
        var perm_file = Path.GetFileName(permission_file_path);

        Console.WriteLine($"install_apk/ target_file:{apk_file}");

        var adbCommands = new List<string> { };
        
        if (install_flag)
        {   //INSTALL
            adbCommands = new List<string>
            {
                "adb -s " + dev_id + " root",
                "adb -s " + dev_id + " remount",
                "adb -s " + dev_id + " shell rm /system/etc/permissions/"+perm_file,
                "adb -s " + dev_id + " push "+permission_file_path+" /system/etc/permissions/",
                "adb -s " + dev_id + " shell rm -rf /system/priv-app/com.example.vtandapp",
                "adb -s " + dev_id + " shell mkdir -p /system/priv-app/com.example.vtandapp",
                "adb -s " + dev_id + " push "+apk_file+" /system/priv-app/com.example.vtandapp/VTApp.apk",
                "adb -s " + dev_id + " shell chmod 644 /system/priv-app/com.example.vtandapp/VTApp.apk",
                "adb -s " + dev_id + " shell chown root:root /system/priv-app/com.example.vtandapp/VTApp.apk",
                "adb -s " + dev_id + " shell oemnv app-enable 0",                               //default app disable
                "adb -s " + dev_id + " reboot"
            };
        }
        else
        {   //REMOVE
            adbCommands = new List<string>
            {
                "adb -s " + dev_id + " root",
                "adb -s " + dev_id + " remount",
                "adb -s " + dev_id + " shell rm /system/etc/permissions/"+perm_file,
                "adb -s " + dev_id + " shell rm -rf /system/priv-app/com.example.vtandapp",
                "adb -s " + dev_id + " shell oemnv app-enable 1",                               //default app enable
                "adb -s " + dev_id + " reboot"
            };
        }


        Console.WriteLine("=== ADB Command Start ===");
        foreach (var adbCommand in adbCommands)
        {
            Console.WriteLine($"[실행 중] {adbCommand}");
            var processInfo = new ProcessStartInfo("cmd.exe", "/c " + adbCommand)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            try
            {
                using (var process = Process.Start(processInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    Console.WriteLine(output);

                    if (!string.IsNullOrWhiteSpace(error))
                        Console.WriteLine(error);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("실행 중 오류 발생: " + ex.Message);
            }
            //Thread.Sleep(2000);
            Console.WriteLine();
        }
        Console.WriteLine($"=== ADB Command Finished. deviceId:{dev_id} ===");
        sharedData.AddDeviceLog(0, $"Android APK Installed. {dev_id}", "DEBUG");
    }

    //private void set_default_app (string dev_id, bool enable)
    //{
    //    var val = enable ? 1 : 0;
    //    string adbCommand = "adb -s "+ dev_id + " shell oemnv app-enable "+val;
    //    Console.WriteLine("adbCommand:" + adbCommand);
    //    var processInfo = new ProcessStartInfo("cmd.exe", "/c " + adbCommand)
    //    {
    //        RedirectStandardOutput = true,
    //        RedirectStandardError = true,
    //        UseShellExecute = false,
    //        CreateNoWindow = true,
    //    };

    //    try
    //    {
    //        using (var process = Process.Start(processInfo))
    //        {
    //            string output = process.StandardOutput.ReadToEnd();
    //            string error = process.StandardError.ReadToEnd();
    //            process.WaitForExit();
    //            Console.WriteLine(output);
    //            Console.WriteLine(error);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine("set_default_app/ error: " + ex.Message);
    //    }

    //}

    private string get_device_id()
    {
        var dev_id = "";

        var processInfo = new ProcessStartInfo
        {
            FileName = "adb",
            Arguments = "devices", // 🔧 -l 옵션 제거 (단순하게)
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using (var process = Process.Start(processInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                Console.WriteLine("=== ADB Output ===");
                Console.WriteLine(output);

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine("=== ADB Error ===");
                    Console.WriteLine(error);
                }

                var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 1; i < lines.Length; i++) // 0번째 라인은 'List of devices attached'
                {
                    var line = lines[i];
                    var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 2 && parts[1] == "device")
                    {
                        string deviceId = parts[0];
                        Console.WriteLine($"🔌 Device: {deviceId}");
                        sharedData.AddDeviceLog(0, $"🔌 Device: {deviceId}", "INFO");
                        
                        dev_id = deviceId; // 마지막 디바이스 ID 사용
                    }
                }

                return dev_id;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ADB 실행 중 오류 발생: " + ex.Message);
            sharedData.AddDeviceLog(0, $"ADB 실행 중 오류 발생: {ex.Message}", "ERROR");
        }

        return "";
    }
    
    /// <summary>
    /// 🔌 첫 번째 연결된 디바이스 ID 반환 (단순화)
    /// </summary>
    private string GetFirstConnectedDevice()
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "adb",
            Arguments = "devices",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using (var process = Process.Start(processInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine("=== ADB Error ===");
                    Console.WriteLine(error);
                }

                var lines = output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i];
                    var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (parts.Length >= 2 && parts[1] == "device")
                    {
                        return parts[0]; // 첫 번째 연결된 디바이스 반환
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("ADB 실행 중 오류 발생: " + ex.Message);
            sharedData.AddDeviceLog(0, $"ADB 실행 중 오류 발생: {ex.Message}", "ERROR");
        }

        return "";
    }

    private void TestAdbCmdButton_Click(object sender, EventArgs e)
    {
        // 🎯 단순화: 첫 번째 디바이스 가져오기
        string dev_id = GetFirstConnectedDevice();

        if (!string.IsNullOrEmpty(dev_id))
        {
            // 🎯 빈 슬롯 자동 할당
            int selectedSlot = FindNextAvailableSlot();
            
            if (selectedSlot == -1)
            {
                // 모든 슬롯이 사용 중이면 연결 거부
                sharedData.AddDeviceLog(0, $"❌ 모든 슬롯이 사용 중입니다. 연결을 거부합니다: {dev_id}", "ERROR");
                sharedData.AddDeviceLog(0, "💡 기존 디바이스를 연결 해제한 후 다시 시도하세요.", "INFO");
                return; // 처리 중단
            }
            
            // 🎯 UnifiedMqttManager에서 고정 포트 가져오기
            int dev_sub_port = unifiedMqtt.GetDevicePort(selectedSlot);
            string topicPrefix = unifiedMqtt.GetDevicePubTopic(selectedSlot);
            
            sharedData.AddDeviceLog(0, $"🎯 디바이스 자동 할당: {dev_id} → 슬롯 {selectedSlot + 1}, Topic: {topicPrefix}, Port: {dev_sub_port}", "INFO");
            
            // 🔧 포트 포워딩, APK 설치, 기본 앱 비활성화
            open_port_forwarding(dev_id, dev_sub_port.ToString());
            install_apk(dev_id, true);


            // 🎯 SharedData에 디바이스를 지정된 슬롯에 배치
            AssignDeviceToSlot(dev_id, selectedSlot);
            
            // 🎯 매핑 정보 출력
            unifiedMqtt.PrintDeviceMappings();
        }
        else
        {
            sharedData.AddDeviceLog(0, "❌ 연결된 디바이스를 찾을 수 없습니다.", "ERROR");
        }
    }
    
    /// <summary>
    /// 🎯 다음 사용 가능한 슬롯 찾기
    /// </summary>
    private int FindNextAvailableSlot()
    {
        // SharedApplicationData에서 빈 슬롯 찾기
        for (int i = 0; i < 4; i++) // 0~3 슬롯
        {
            var device = sharedData.GetDeviceByIndex(i);
            if (device != null && !device.IsConnected)
            {
                return i; // 빈 슬롯 발견
            }
        }
        return -1; // 모든 슬롯이 사용 중
    }
    

    
    /// <summary>
    /// 🎯 디바이스를 지정된 슬롯에 배치 (SharedApplicationData 사용)
    /// </summary>
    private void AssignDeviceToSlot(string deviceId, int slotIndex)
    {
        // SharedApplicationData의 중앙 관리 기능 사용
        sharedData.AssignDeviceToSlot(deviceId, slotIndex);
        
        // UI 갱신
        RefreshAllControlsUI();
    }

    private void TestDbButton_Click(object sender, EventArgs e)
    {
        string connectionString = "Server=127.0.0.1;Database=test1;User ID=test1;Password=test1;Charset=utf8;";
        //Console.WriteLine(connectionString);

        using (MySqlConnection conn = new MySqlConnection(connectionString))
        {
            try
            {
                string dev_id = get_device_id();
                conn.Open();
                string sql = $"SELECT device_id, error_reason FROM vtapp_result WHERE device_id = '{dev_id}'";
                Console.WriteLine(sql);

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.HasRows)
                    {
                        Console.WriteLine($"No record found {dev_id}");
                    }
                    else
                    {
                        while (reader.Read())
                        {
                            string deviceId = reader["device_id"].ToString();
                            string errorReason = reader["error_reason"].ToString();

                            Console.WriteLine($"device_id: {deviceId}, error_reason: {errorReason}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DB 오류: " + ex.Message);
            }
        }
    }


    // 🎯 JSON 로드 테스트 함수
    private void TestJsonLoad()
    {
        try
        {
            // 🔍 간단한 경로 설정 - Resources 폴더는 빌드 시 자동으로 복사됨
            string jsonFilePath = Path.Combine(Application.StartupPath, "Resources", "st9730_config_1.0.json");
            
            sharedData.AddDeviceLog(0, $"🔍 JSON 파일 경로: {jsonFilePath}", "DEBUG");
            
            // 파일 존재 확인
            if (!File.Exists(jsonFilePath))
            {
                sharedData.AddDeviceLog(0, $"❌ JSON 파일을 찾을 수 없습니다: {jsonFilePath}", "ERROR");
                
                // Resources 폴더 내용 확인
                string resourcesPath = Path.Combine(Application.StartupPath, "Resources");
                if (Directory.Exists(resourcesPath))
                {
                    sharedData.AddDeviceLog(0, $"📁 Resources 폴더 내용:", "DEBUG");
                    var files = Directory.GetFiles(resourcesPath);
                    foreach (var file in files)
                    {
                        sharedData.AddDeviceLog(0, $"  - {Path.GetFileName(file)}", "DEBUG");
                    }
                }
                else
                {
                    sharedData.AddDeviceLog(0, $"❌ Resources 폴더가 존재하지 않습니다: {resourcesPath}", "ERROR");
                }
                return;
            }
            
            sharedData.AddDeviceLog(0, $"📄 JSON 파일 로드 시작: {jsonFilePath}", "INFO");
            
                         // JSON 로드 실행
             bool loadSuccess = sharedData.LoadPeriTestFromJson(jsonFilePath);
             
             if (loadSuccess)
             {
                 sharedData.AddDeviceLog(0, "✅ JSON 로드 테스트 성공!", "SUCCESS");
                 
                 // 로드된 데이터 확인
                 var device = sharedData.GetDeviceByIndex(0);
                 if (device != null)
                 {
                     sharedData.AddDeviceLog(0, $"📝 로드된 IMEI: {device.IMEI_SN_Data.IMEI}", "INFO");
                     sharedData.AddDeviceLog(0, $"📝 로드된 SN: {device.IMEI_SN_Data.SerialNumber}", "INFO");
                     sharedData.AddDeviceLog(0, $"🌐 로드된 APN: {device.ApnInfo.APN}", "INFO");
                     sharedData.AddDeviceLog(0, $"🌐 로드된 APN ID: {device.ApnInfo.UserID}", "INFO");
                     sharedData.AddDeviceLog(0, $"🧪 Auto Test 개수: {device.PeripheralData.AutoTests.Count}", "INFO");
                     sharedData.AddDeviceLog(0, $"🔧 Manual Test 개수: {device.PeripheralData.ManualTests.Count}", "INFO");
                 }
                 
                 // 🎯 JSON 로드 성공 시에만 전체 UI 갱신 (테스트 항목이 바뀌었으므로)
                 sharedData.AddDeviceLog(0, "🔄 JSON 로드 완료 - 전체 UI 갱신 시작", "INFO");
                 RefreshAllControlsUI();
             }
             else
             {
                 sharedData.AddDeviceLog(0, "❌ JSON 로드 테스트 실패!", "ERROR");
             }
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(0, $"❌ JSON 로드 테스트 중 오류: {ex.Message}", "ERROR");
        }
    }

    // 🌐 통합 데이터 접근자 (외부에서 사용 가능)
    public SharedApplicationData GetSharedData() => sharedData;

    // 🔧 개별 컨트롤에 특정 데이터 설정하는 예시 메서드들
    public void SetImeiSnWriteData(int deviceIndex, string imei, string serialNumber, string model)
    {
        sharedData.Set_IMEI_SN_Data(deviceIndex, imei, serialNumber, model);
        sharedData.AddDeviceLog(deviceIndex, $"SetImeiSnWriteData 데이터 설정: Device #{deviceIndex}", "INFO");
    }

    public void UpdateTestStatus(int deviceIndex, int testIndex, string newStatus, bool isAutoTest = true)
    {
        sharedData.UpdateTestStatus(deviceIndex, testIndex, newStatus, isAutoTest);
        sharedData.AddDeviceLog(deviceIndex, $"🔄 테스트 상태 업데이트: Device #{deviceIndex}", "INFO");
    }

    // 🧪 개별 테스트 함수 추가 메서드들 (Form에서 커스터마이징 가능) - string 반환으로 변경
    public void AddAutoTestFunction(int deviceIndex, string status, string testName, Func<Task<FunctionTestResult>> testFunction)
    {
        sharedData.AddAutoTestFunction(deviceIndex, status, testName, testFunction);
        sharedData.AddDeviceLog(deviceIndex, $"🤖 Auto Test 함수 추가: {testName}", "INFO");
    }

    public void AddManualTestFunction(int deviceIndex, string status, string testName, Func<Task<FunctionTestResult>> testFunction)
    {
        sharedData.AddManualTestFunction(deviceIndex, status, testName, testFunction);
        sharedData.AddDeviceLog(deviceIndex, $"🔧 Manual Test 함수 추가: {testName}", "INFO");
    }

    // 🧪 실제 테스트 함수들 (Form1에서 정의) - string 반환으로 변경
    
    // Camera Test 함수
    private async Task<FunctionTestResult> TestCameraFunction(int deviceIndex)
    {
        sharedData.AddDeviceLog(deviceIndex, "📷 카메라 테스트 시작...", "INFO");
        
        // 실제 카메라 테스트 로직
        await Task.Delay(1500); // 카메라 초기화 시뮬레이션
        
        // 90% 성공률로 시뮬레이션
        bool success = new Random().Next(0, 10) < 9;
        
        if (success)
        {
            sharedData.AddDeviceLog(deviceIndex, "📷 카메라 테스트 성공", "SUCCESS");
            return new FunctionTestResult { Result = true, Description = "카메라 기능이 정상적으로 작동합니다.\n- 전면/후면 카메라 인식됨\n- 촬영 기능 정상\n- 포커스 조절 가능" };
        }
        else
        {
            sharedData.AddDeviceLog(deviceIndex, "📷 카메라 테스트 실패", "ERROR");
            return new FunctionTestResult { Result = false, Description = "카메라 테스트 실패:\n- 카메라 모듈 인식 오류\n- 권한 설정 필요\n- 하드웨어 점검 요망"};
        }
    }

    // WiFi Test 함수
    private async Task<FunctionTestResult> TestWiFiFunction(int deviceIndex)
    {
        sharedData.AddDeviceLog(deviceIndex, "📶 WiFi 테스트 시작...", "INFO");
        
        // 실제 WiFi 테스트 로직
        await Task.Delay(2000); // WiFi 연결 테스트 시뮬레이션
        
        bool success = new Random().Next(0, 10) < 8; // 80% 성공률
        
        if (success)
        {
            sharedData.AddDeviceLog(deviceIndex, "📶 WiFi 테스트 성공", "SUCCESS");
            return new FunctionTestResult { Result = true, Description = "WiFi 연결이 정상적으로 작동합니다.\n- 신호 강도: 양호\n- 연결 속도: 54Mbps\n- 네트워크 설정 완료" };
        }
        else
        {
            sharedData.AddDeviceLog(deviceIndex, "📶 WiFi 테스트 실패", "ERROR");
            return new FunctionTestResult { Result = false, Description = "WiFi 테스트 실패:\n- 네트워크 연결 불가\n- 신호 강도 약함\n- WiFi 모듈 점검 필요" };
        }
    }

    // Audio Test 함수
    private async Task<FunctionTestResult> TestAudioFunction(int deviceIndex)
    {
        sharedData.AddDeviceLog(deviceIndex, "🎧 오디오 테스트 시작...", "INFO");
        
        // 실제 오디오 테스트 로직
        await Task.Delay(1800);
        
        bool success = new Random().Next(0, 10) < 8;
        
        if (success)
        {
            sharedData.AddDeviceLog(deviceIndex, "🎧 오디오 테스트 성공", "SUCCESS");
            return new FunctionTestResult { Result = true, Description = "오디오 시스템이 정상적으로 작동합니다.\n- 스피커 출력: 정상\n- 마이크 입력: 감지됨\n- 음질: 양호"};
        }
        else
        {
            sharedData.AddDeviceLog(deviceIndex, "🎧 오디오 테스트 실패", "ERROR");
            return new FunctionTestResult { Result = false, Description = "오디오 테스트 실패:\n- 스피커 출력 없음\n- 마이크 인식 오류\n- 오디오 드라이버 점검 필요" };
        }
    }

    // Display Test 함수
    private async Task<FunctionTestResult> TestDisplayFunction(int deviceIndex)
    {
        sharedData.AddDeviceLog(deviceIndex, "📺 디스플레이 테스트 시작...", "INFO");
        
        // 실제 디스플레이 테스트 로직
        await Task.Delay(2500);
        
        bool success = new Random().Next(0, 10) < 9;
        
        if (success)
        {
            sharedData.AddDeviceLog(deviceIndex, "📺 디스플레이 테스트 성공", "SUCCESS");
            return new FunctionTestResult { Result = true, Description = "디스플레이가 정상적으로 작동합니다.\n- 해상도: 1920x1080\n- 색상 표현: 정상\n- 밝기 조절: 가능" };
        }
        else
        {
            sharedData.AddDeviceLog(deviceIndex, "📺 디스플레이 테스트 실패", "ERROR");
            return new FunctionTestResult { Result = false, Description = "디스플레이 테스트 실패:\n- 화면 출력 불안정\n- 색상 왜곡 발생\n- 그래픽 드라이버 업데이트 필요" };
        }
    }

    // Touch Test 함수
    private async Task<FunctionTestResult> TestTouchFunction(int deviceIndex)
    {
        sharedData.AddDeviceLog(deviceIndex, "👆 터치 테스트 시작...", "INFO");
        
        // 실제 터치 테스트 로직
        await Task.Delay(1000);
        
        bool success = new Random().Next(0, 10) < 9;
        
        if (success)
        {
            sharedData.AddDeviceLog(deviceIndex, "👆 터치 테스트 성공", "SUCCESS");
            return new FunctionTestResult { Result = true, Description = "터치 스크린이 정상적으로 작동합니다.\n- 터치 감도: 양호\n- 멀티터치: 지원\n- 제스처 인식: 정상"};
        }
        else
        {
            sharedData.AddDeviceLog(deviceIndex, "👆 터치 테스트 실패", "ERROR");
            return new FunctionTestResult { Result = false, Description = "터치 테스트 실패:\n- 터치 반응 없음\n- 터치 좌표 오류\n- 터치 패널 캘리브레이션 필요" };
        }
    }

    // 🔍 설정 파일 로드 및 데이터 적용
    private void LoadConfigurationFile(string filePath)
    { 
        try
        {
            sharedData.AddDeviceLog(0, $"📄 설정 파일 로드 시작: {Path.GetFileName(filePath)}", "INFO");
            
            // SharedApplicationData를 통해 JSON 로드
            bool loadSuccess = sharedData.LoadPeriTestFromJson(filePath);
            
            if (loadSuccess)
            {
                sharedData.AddDeviceLog(0, $"✅ 설정 파일 로드 성공!", "SUCCESS");
                
                // 🎯 로드 성공 시 전체 UI 갱신
                RefreshAllControlsUI();
                
                // 기본 정보 로그
                var dbAccessId = Config.Instance.GetNested("database.access_info.db_access_id") as string;
                var model = Config.Instance.GetNested("model") as string;
                var version = Config.Instance.GetNested("fw_version") as string;
                sharedData.AddDeviceLog(0, $"📝 DB Access ID: {dbAccessId}", "INFO");
                sharedData.AddDeviceLog(0, $"📱 모델: {model}", "INFO");
                this.modelNameTextBox.Text = $"Model:{model},   ver:{version}";
            }
            else
            {
                sharedData.AddDeviceLog(0, $"❌ 설정 파일 로드 실패!", "ERROR");
                MessageBox.Show(this, "설정 파일을 로드할 수 없습니다.\n파일 형식이나 내용을 확인해주세요.", 
                              "설정 로드 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(0, $"❌ 설정 파일 로드 중 오류: {ex.Message}", "ERROR");
            MessageBox.Show(this, $"설정 파일 로드 중 오류가 발생했습니다:\n{ex.Message}", 
                          "설정 로드 오류", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // 🔍 마지막으로 사용한 설정 파일 자동 로드
            #region 🧪 Test Mode Communication 기능
        
        /// <summary>
        /// 🧪 Test Mode일 때 상단바에 Communication 버튼들 추가
        /// </summary>
        private void ShowTestModeButtons()
        {
            try
            {
                // CommunicationExamples 초기화
                if (communicationExamples == null)
                {
                    communicationExamples = new AndroidDeviceVerificationTool.Examples.CommunicationExamples();
                }
                
                // 버튼 기본 설정
                var buttonStyle = new Action<Button>((btn) =>
                {
                    btn.BackColor = Color.FromArgb(40, 167, 69);
                    btn.ForeColor = Color.White;
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.Font = new Font("Noto Sans KR", 8F, FontStyle.Regular);
                    btn.Size = new Size(80, 30);
                });
                
                // 패턴 1 버튼
                btnPattern1 = new Button();
                btnPattern1.Text = "📤패턴1";
                btnPattern1.Location = new Point(550, 5);
                buttonStyle(btnPattern1);
                btnPattern1.Click += async (s, e) => await BtnPattern1_Click();
                
                // 패턴 2 버튼
                btnPattern2 = new Button();
                btnPattern2.Text = "🔥패턴2";
                btnPattern2.Location = new Point(640, 5);
                buttonStyle(btnPattern2);
                btnPattern2.Click += async (s, e) => await BtnPattern2_Click();
                
                // 패턴 3 버튼
                btnPattern3 = new Button();
                btnPattern3.Text = "📲패턴3";
                btnPattern3.Location = new Point(730, 5);
                buttonStyle(btnPattern3);
                btnPattern3.Click += async (s, e) => await BtnPattern3_Click();
                
                // 패턴 4 버튼
                btnPattern4 = new Button();
                btnPattern4.Text = "📡패턴4";
                btnPattern4.Location = new Point(820, 5);
                buttonStyle(btnPattern4);
                btnPattern4.Click += async (s, e) => await BtnPattern4_Click();
                
                // 전체 테스트 버튼
                btnTestAll = new Button();
                btnTestAll.Text = "🚀전체";
                btnTestAll.Location = new Point(910, 5);
                btnTestAll.BackColor = Color.FromArgb(220, 53, 69);
                btnTestAll.ForeColor = Color.White;
                btnTestAll.FlatStyle = FlatStyle.Flat;
                btnTestAll.FlatAppearance.BorderSize = 0;
                btnTestAll.Font = new Font("Noto Sans KR", 8F, FontStyle.Bold);
                btnTestAll.Size = new Size(80, 30);
                btnTestAll.Click += async (s, e) => await BtnTestAll_Click();
                
                // 상단바에 추가
                this.titlePanel.Controls.Add(btnPattern1);
                this.titlePanel.Controls.Add(btnPattern2);
                this.titlePanel.Controls.Add(btnPattern3);
                this.titlePanel.Controls.Add(btnPattern4);
                this.titlePanel.Controls.Add(btnTestAll);
                
                sharedData.AddDeviceLog(0, "🧪 Test Mode: Communication 버튼들이 상단바에 추가되었습니다", "INFO");
            }
            catch (Exception ex)
            {
                sharedData?.AddDeviceLog(0, $"❌ Test Mode 버튼 추가 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 🏭 Production Mode일 때 상단바에서 Communication 버튼들 제거
        /// </summary>
        private void HideTestModeButtons()
        {
            try
            {
                if (btnPattern1 != null) { this.titlePanel.Controls.Remove(btnPattern1); btnPattern1.Dispose(); btnPattern1 = null; }
                if (btnPattern2 != null) { this.titlePanel.Controls.Remove(btnPattern2); btnPattern2.Dispose(); btnPattern2 = null; }
                if (btnPattern3 != null) { this.titlePanel.Controls.Remove(btnPattern3); btnPattern3.Dispose(); btnPattern3 = null; }
                if (btnPattern4 != null) { this.titlePanel.Controls.Remove(btnPattern4); btnPattern4.Dispose(); btnPattern4 = null; }
                if (btnTestAll != null) { this.titlePanel.Controls.Remove(btnTestAll); btnTestAll.Dispose(); btnTestAll = null; }
                
                sharedData?.AddDeviceLog(0, "🏭 Production Mode: Communication 버튼들이 제거되었습니다", "INFO");
            }
            catch (Exception ex)
            {
                sharedData?.AddDeviceLog(0, $"❌ Test Mode 버튼 제거 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 📤 패턴 1: PC → Android (AWAIT) 테스트 - 🎯 디바이스 0 (dev_1, 포트 18831)
        /// </summary>
        private async Task BtnPattern1_Click()
        {
            try
            {
                sharedData.AddDeviceLog(0, "🎯 패턴1: 디바이스 0 → dev_1 토픽 (포트 18831)", "INFO");

                var response = await unifiedMqtt.SendToDevice(0, "test_camera", new
                {
                    timeout = 30000,
                    test_params = "front_camera",
                    quality = "1080p"
                });
                
                sharedData.AddDeviceLog(0, $"Response: {response.Status} - {response.Message}", "SUCCESS");
                sharedData.AddDeviceLog(0, $"🎯 사용된 토픽: {unifiedMqtt.GetDevicePubTopic(0)}, 포트: {unifiedMqtt.GetDevicePort(0)}", "INFO");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ 패턴1 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 🔥 패턴 2: PC → Android (Fire-and-Forget) 테스트 - 🎯 디바이스 1 (dev_2, 포트 18832)
        /// </summary>
        private async Task BtnPattern2_Click()
        {
            try
            {
                sharedData.AddDeviceLog(0, "🎯 패턴2: 디바이스 1 → dev_2 토픽 (포트 18832)", "INFO");
                
                // 🎯 고정 매핑 사용: 디바이스 1 → dev_2 토픽, 포트 18832
                unifiedMqtt.SendToDeviceFireAndForget(1, "test_microphone");
                
                sharedData.AddDeviceLog(0, $"🔥 Fire-and-Forget 전송 완료!", "SUCCESS");
                sharedData.AddDeviceLog(0, $"🎯 사용된 토픽: {unifiedMqtt.GetDevicePubTopic(1)}, 포트: {unifiedMqtt.GetDevicePort(1)}", "INFO");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ 패턴2 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 📲 패턴 3: Android → PC (AWAIT) 테스트 - 🎯 디바이스 2 (dev_3, 포트 18833)
        /// </summary>
        private async Task BtnPattern3_Click()
        {
            try
            {
                sharedData.AddDeviceLog(0, "🎯 패턴3: 디바이스 2 → dev_3 토픽 (포트 18833)", "INFO");
                
                // 🎯 고정 매핑 사용: 디바이스 2 → dev_3 토픽, 포트 18833
                var response = await unifiedMqtt.SendToDevice(2, "test_wifi");
                
                sharedData.AddDeviceLog(0, $"📋 응답: {response.Status} - {response.Message}", "SUCCESS");
                sharedData.AddDeviceLog(0, $"🎯 사용된 토픽: {unifiedMqtt.GetDevicePubTopic(2)}, 포트: {unifiedMqtt.GetDevicePort(2)}", "INFO");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ 패턴3 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 📡 패턴 4: Android → PC (Events) 테스트 - 🎯 디바이스 3 (dev_4, 포트 18834)
        /// </summary>
        private async Task BtnPattern4_Click()
        {
            try
            {
                sharedData.AddDeviceLog(0, "🎯 패턴4: 디바이스 3 → dev_4 토픽 (포트 18834)", "INFO");
                
                // 🎯 고정 매핑 사용: 디바이스 3 → dev_4 토픽, 포트 18834
                var response = await unifiedMqtt.SendToDevice(3, "test_display");
                
                sharedData.AddDeviceLog(0, $"📋 응답: {response.Status} - {response.Message}", "SUCCESS");
                sharedData.AddDeviceLog(0, $"🎯 사용된 토픽: {unifiedMqtt.GetDevicePubTopic(3)}, 포트: {unifiedMqtt.GetDevicePort(3)}", "INFO");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ 패턴4 오류: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// 🚀 모든 패턴 테스트 - 🎯 전체 디바이스 고정 매핑 테스트
        /// </summary>
        private async Task BtnTestAll_Click()
        {
            try
            {
                sharedData.AddDeviceLog(0, "🚀 전체 디바이스 고정 매핑 테스트 시작!", "INFO");
                
                // 🎯 매핑 정보 출력
                unifiedMqtt.PrintDeviceMappings();
                
                // 🎯 각 디바이스별로 순차 테스트
                for (int i = 0; i < 4; i++)
                {
                    try
                    {
                        sharedData.AddDeviceLog(0, $"🎯 디바이스 {i} 테스트 중...", "INFO");
                        
                        var response = await unifiedMqtt.SendToDevice(i, $"test_device_{i}");
                        
                        sharedData.AddDeviceLog(0, $"✅ 디바이스 {i}: {response.Status} - {response.Message}", "SUCCESS");
                        
                        // 1초 대기
                        await Task.Delay(1000);
                    }
                    catch (Exception deviceEx)
                    {
                        sharedData.AddDeviceLog(0, $"❌ 디바이스 {i} 테스트 실패: {deviceEx.Message}", "ERROR");
                    }
                }
                
                sharedData.AddDeviceLog(0, "🎉 전체 디바이스 테스트 완료!", "SUCCESS");
            }
            catch (Exception ex)
            {
                sharedData.AddDeviceLog(0, $"❌ 전체 테스트 오류: {ex.Message}", "ERROR");
            }
        }
        
        #endregion
        
        private void LoadLastUsedConfigurationFile()
        {
        try
        {
            string lastUsedPath = Config.Instance.GetLastUsedConfigPath();
            
            if (!string.IsNullOrEmpty(lastUsedPath) && File.Exists(lastUsedPath))
            {
                sharedData.AddDeviceLog(0, $"🔄 마지막 사용 설정 파일 자동 로드: {Path.GetFileName(lastUsedPath)}", "INFO");
                LoadConfigurationFile(lastUsedPath);
            }
            else
            {
                sharedData.AddDeviceLog(0, "ℹ️ 마지막 사용 설정 파일이 없습니다. 기본 설정을 사용합니다.", "INFO");
            }
        }
        catch (Exception ex)
        {
            sharedData.AddDeviceLog(0, $"❌ 마지막 설정 파일 로드 중 오류: {ex.Message}", "ERROR");
        }
    }
}

