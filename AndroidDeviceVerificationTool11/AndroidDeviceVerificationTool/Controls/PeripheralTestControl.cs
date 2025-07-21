using AndroidDeviceVerificationTool.Data;
using AndroidDeviceVerificationTool.Design;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static AndroidDeviceVerificationTool.Design.MaterialDesign;

namespace AndroidDeviceVerificationTool.Controls
{
    /// <summary>
    /// 🎯 PeripheralTest 컨트롤 - 1600x900 크기 최적화
    /// 4개 디바이스 고정 레이아웃, SharedApplicationData 중심 제어
    /// </summary>
    public partial class PeripheralTestControl : UserControl
    {
        // 🚀 Windows API - 스크롤바 완전 제거
        [DllImport("user32.dll")]
        private static extern int ShowScrollBar(IntPtr hWnd, int wBar, bool bShow);
        
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        
        private const int GWL_STYLE = -16;
        private const int WS_VSCROLL = 0x00200000;
        private const int WS_HSCROLL = 0x00100000;
        private const int SB_BOTH = 3;
        
        // 🚀 스크롤바 없는 커스텀 패널 클래스
        public class ScrollablePanel : Panel
        {
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.Style &= ~WS_HSCROLL;
                    cp.Style &= ~WS_VSCROLL;
                    return cp;
                }
            }

            protected override void WndProc(ref Message m)
            {
                const int WM_HSCROLL = 0x114;
                const int WM_VSCROLL = 0x115;
                
                // 스크롤 메시지는 처리하되 스크롤바는 그리지 않음
                if (m.Msg == WM_HSCROLL || m.Msg == WM_VSCROLL)
                {
                    base.WndProc(ref m);
                    return;
                }
                
                base.WndProc(ref m);
            }
            
            // 스크롤바를 그리지 않도록 오버라이드
            protected override void OnPaintBackground(PaintEventArgs e)
            {
                base.OnPaintBackground(e);
            }
        }
        
        // 🎯 스크롤바 완전 제거 헬퍼 메서드 (백업용)
        private void HideScrollBars(Panel panel)
        {
            if (panel.Handle != IntPtr.Zero)
            {
                // Windows API로 스크롤바 완전 숨김
                ShowScrollBar(panel.Handle, SB_BOTH, false);
                
                // 윈도우 스타일에서 스크롤 플래그 제거
                int style = GetWindowLong(panel.Handle, GWL_STYLE);
                style &= ~(WS_VSCROLL | WS_HSCROLL);
                SetWindowLong(panel.Handle, GWL_STYLE, style);
            }
        }




        // 🌐 통합 데이터 관리자 (모든 컨트롤이 공유)
        private SharedApplicationData sharedData;
        private Data.UnifiedMqttManager unifiedMqtt;

        // UI 컨테이너들
        private Panel mainPanel;
        private List<Panel> devicePanels = new List<Panel>();
        private bool isInitialized = false;

        // 🔧 Save 버튼 콜백 함수 (폼에서 설정 가능)
        public Action<int> OnSaveButtonClicked;

        // 🎨 1600x900 크기 최적화 상수들 (4개 디바이스 고정) - 여백 최소화로 더 넓게 분배
        private const int DEVICE_WIDTH = 375;      // 360 → 375로 늘림 (더 넓게)
        private const int DEVICE_HEIGHT = 815;     // 820 → 835로 늘림 (아래 여백 줄임)
        private const int DEVICE_SPACING = 15;     // 12 → 15로 늘림 (더 넓은 간격)
        private const int START_MARGIN = 25;       // 20 → 25로 늘림 (좌우 균형)
        private const int AUTO_TEST_HEIGHT = 300;  // Auto Test 영역 높이
        private const int MANUAL_TEST_HEIGHT = 300; // Manual Test 영역 높이

        public PeripheralTestControl()
        {
            // 🎨 **더블버퍼링 활성화** - 깜빡임 완전 제거
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, false);
            
            // InitializeComponent() 제거 - 완전히 코드로 UI 생성
            sharedData = SharedApplicationData.Instance;
            unifiedMqtt = UnifiedMqttManager.Instance;

            CreateMainUI();
            SubscribeToSharedDataEvents();
            
            isInitialized = true;
            
            RefreshUI();
            
        }

        // 🎨 메인 UI 생성 (1600x900 4개 고정 레이아웃)
        private void CreateMainUI()
        {
            // this.Padding = new Padding(0, 0,0,0);
            // this.BackColor = Color.FromArgb(47, 47, 55);
            // this.Size = new Size(1600, 875); // 860 → 875로 늘림 (아래 여백 줄임)

            // 메인 패널 (고정 4개 디바이스 레이아웃) - 더블버퍼링 적용
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(47, 47, 55),
                AutoScroll = false,  // 가로 스크롤 완전 비활성화
                HorizontalScroll = { Enabled = false, Visible = false }, // 가로 스크롤 강제 비활성화
                VerticalScroll = { Enabled = false, Visible = false }   // 세로 스크롤도 메인에서 비활성화
            };
            
            // 🎨 **메인 패널에 더블버퍼링 적용**
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, mainPanel, new object[] { true });

            this.Controls.Add(mainPanel);

            // 🚀 기본 4개 디바이스 보장 (Not Connected 상태) - AddDevice 없이 고정 슬롯만 표시
            EnsureDefaultDevices();

            // 4개 디바이스 패널 고정 생성
            CreateFixedDevicePanels();
            
        }



        // 🚀 기본 4개 디바이스 보장 (Not Connected 상태) - AddDevice 없이 고정 슬롯만 표시
        private void EnsureDefaultDevices()
        {
            // SharedData는 건드리지 않음 - 순수하게 UI만 4개 고정 슬롯 표시
            // 실제 디바이스 연결시에만 SharedData에 추가됨
            
            // 로그는 제거 - 기본 슬롯 표시는 로그가 필요 없음
        }

        // 📱 4개 디바이스 패널 고정 생성
        private void CreateFixedDevicePanels()
        {
            devicePanels.Clear();
            mainPanel.Controls.Clear();

            for (int i = 0; i < 4; i++)
            {
                int xPos = START_MARGIN + (i * (DEVICE_WIDTH + DEVICE_SPACING));
                
                var devicePanel = new Panel
                {
                    Location = new Point(xPos, 10), // 15 → 10으로 조정 (위쪽 여백 줄임)
                    Size = new Size(DEVICE_WIDTH, DEVICE_HEIGHT),
                    BackColor = Color.FromArgb(47, 47, 55),
                    BorderStyle = BorderStyle.None, 
                    AutoScroll = true,  // 각 디바이스 패널에서만 세로 스크롤
                    HorizontalScroll = { Enabled = false, Visible = false }, // 가로 스크롤 비활성화
                    Tag = i
                };

                // 🎨 **각 디바이스 패널에 더블버퍼링 적용**
                typeof(Panel).InvokeMember("DoubleBuffered",
                    BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, devicePanel, new object[] { true });


                // 🎯 스크롤바 완전히 숨기고 가로 스크롤 완전 비활성화
                devicePanel.AutoScroll = true;                       // 세로 스크롤만 활성화
                devicePanel.VerticalScroll.Visible = false;          // 세로 스크롤바 숨기기
                devicePanel.HorizontalScroll.Visible = false;        // 가로 스크롤바 숨기기
                devicePanel.HorizontalScroll.Enabled = false;        // 가로 스크롤 완전 비활성화
                devicePanel.AutoScrollMinSize = new Size(0, 0);      // 가로 스크롤 생성 방지

                mainPanel.Controls.Add(devicePanel);
                devicePanels.Add(devicePanel);
            }

            // 전체 계산 확인: 25 + (375 + 15) * 4 = 25 + 1560 = 1585 < 1600 ✓ (15px 여유)
        }

        // 🌐 통합 데이터 이벤트 구독
        private void SubscribeToSharedDataEvents()
        {
            sharedData.DeviceDataChanged += OnSharedDeviceDataChanged;
            sharedData.DeviceConnectionChanged += OnSharedDeviceConnectionChanged;
            sharedData.DeviceAdded += OnSharedDeviceAdded;
            sharedData.DeviceRemoved += OnSharedDeviceRemoved;
            sharedData.DeviceLogAdded += OnDeviceLogAdded;
            sharedData.ModelNameChanged += OnModelNameChanged;
        }

        private void OnSharedDeviceDataChanged(int index, UnifiedDeviceData device)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSharedDeviceDataChanged(index, device)));
                return;
            }
            
            // ✅ 인덱스 통일: 이제 Device Index와 Panel Index 모두 0-based
            int panelIndex = index; // index - 1 제거
            if (panelIndex >= 0 && panelIndex < 4)
            {
                // 🎯 **완전히 조용한 처리** - 로그도 생성하지 않음
                // sharedData.AddDeviceLog(index, $"🎯 PeripheralTest: Device #{index} 데이터 변경됨", "DEBUG");
                
                // 🚫 **전체 UI 새로고침 완전 차단** - 버튼 상태만 조용히 업데이트
                var devicePanel = devicePanels[panelIndex];
                if (devicePanel.Controls.Count > 0 && device != null)
                {
                    // 버튼 상태만 조용히 업데이트 (UI 새로고침 없음)
                    UpdateManualTestButtonStates(devicePanel, panelIndex, device);
                }
                // 🚫 전체 UI 새로고침 완전 제거
                // RefreshDevicePanel(panelIndex); // 절대 호출하지 않음
            }
        }

        private void OnSharedDeviceConnectionChanged(int index, UnifiedDeviceData device)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSharedDeviceConnectionChanged(index, device)));
                return;
            }
            
            // ✅ 인덱스 통일: 이제 Device Index와 Panel Index 모두 0-based
            int panelIndex = index; // index - 1 제거
            if (panelIndex >= 0 && panelIndex < 4)
            {
                RefreshDevicePanel(panelIndex);
            }
        }

        private void OnSharedDeviceAdded(UnifiedDeviceData device)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSharedDeviceAdded(device)));
                return;
            }
            
            // 🚫 **디바이스 추가 시 UI 새로고침 완전 차단**
            // 연결 상태 변경 시에만 UI 업데이트되므로 추가 시점에는 불필요
            // RefreshUI(); // 제거
            // RefreshDevicePanel(panelIndex); // 제거
            
            // 🎯 완전히 조용한 처리 - 로그도 생성하지 않음
            // sharedData.AddDeviceLog(device.Index, $"✅ PeripheralTest: Device #{device.Index} 추가됨", "INFO");
        }

        private void OnSharedDeviceRemoved(int index)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSharedDeviceRemoved(index)));
                return;
            }
            
            // 🚫 **디바이스 제거 시에도 UI 새로고침 차단**
            // 연결 해제 이벤트에서 이미 처리되므로 제거 시점에는 불필요
            // RefreshUI(); // 제거
            
            // 🎯 완전히 조용한 처리
            // sharedData.AddDeviceLog(index, $"✅ PeripheralTest: Device #{index} 제거됨", "INFO");
        }

        private void OnDeviceLogAdded(int deviceIndex, string message, string level)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDeviceLogAdded(deviceIndex, message, level)));
                return;
            }
            
            // 해당 디바이스의 로그창 업데이트 (인덱스 통일)
            int panelIndex = deviceIndex; // deviceIndex - 1 제거
            
            if (panelIndex >= 0 && panelIndex < 4 && panelIndex < devicePanels.Count)
            {
                UpdateDeviceLog(panelIndex, message, level);
            }
        }

        private void OnModelNameChanged(string modelName)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnModelNameChanged(modelName)));
                return;
            }
            
            // PeripheralTestControl에서는 특별한 UI 업데이트가 필요 없음
            // 로그로만 확인
            sharedData?.AddDeviceLog(0, $"🏷️ PeripheralTest: 모델명 변경됨 - '{modelName}'", "INFO");
        }

        private void UpdateDeviceLog(int panelIndex, string message, string level)
        {
            var devicePanel = devicePanels[panelIndex];
            if (devicePanel == null) return;

            // LogPanel 직접 찾기 (간단한 방법)
            Panel? logPanel = FindLogPanel(devicePanel);
            
            if (logPanel != null)
            {
                // 새 로그 추가 (타임스탬프 포함)
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                string newLogLine = $"[{timestamp}] {message}";
                
                // 레벨에 따른 색상 설정
                Color logColor = level.ToUpper() switch
                {
                    "SUCCESS" => Color.FromArgb(0x00, 0xFF, 0xA6), // 초록색
                    "ERROR" => Color.FromArgb(0xFF, 0x40, 0x40),   // 빨간색
                    "WARN" => Color.FromArgb(0xFF, 0xA5, 0x00),    // 주황색
                    "DEBUG" => Color.FromArgb(0xD9, 0xD9, 0xD9),   // 회색
                    _ => Color.White                                // 기본 흰색
                };

                // Panel에 새 로그 라벨 추가
                AddLogToPanel(logPanel, newLogLine, logColor);
            }
        }

        // 🔍 LogPanel을 재귀적으로 찾는 헬퍼 메서드
        private Panel? FindLogPanel(Control parent)
        {
            // 현재 컨트롤이 LogPanel인지 확인
            if (parent is Panel panel && parent.Name.StartsWith("LogPanel"))
            {
                return panel;
            }

            // 자식 컨트롤들을 재귀적으로 검색
            foreach (Control child in parent.Controls)
            {
                var found = FindLogPanel(child);
                if (found != null) return found;
            }

            return null;
        }

        // 🔄 전체 UI 새로고침
        public void RefreshUI()
        {
            if (!isInitialized) return;

            // 🎯 RefreshUI 로그 제거 - 과도한 디버그 정보
            // sharedData?.AddDeviceLog(0, $"🔄 PeripheralTestControl RefreshUI 시작", "DEBUG");

            // 🎯 VD #01 강제 체크 로그 제거 - 불필요한 디버그 정보
            // if (sharedData?.Devices.Count > 0) { ... }

            for (int i = 0; i < 4; i++)
            {
                RefreshDevicePanel(i);
            }
            
            // 🎯 RefreshUI 완료 로그 제거 - 과도한 디버그 정보
            // sharedData?.AddDeviceLog(0, $"✅ PeripheralTestControl RefreshUI 완료", "DEBUG");
        }

        // 🔄 개별 디바이스 패널 새로고침 - 타이밍 이슈 해결
        private void RefreshDevicePanel(int panelIndex)
        {
            if (panelIndex >= devicePanels.Count) return;

            var devicePanel = devicePanels[panelIndex];
            
            // 🔍 타이밍 이슈 해결: 디바이스 상태를 안전하게 확인
            UnifiedDeviceData device = null;
            bool hasValidDevice = false;
            
            // SharedData가 준비되었는지 확인
            if (sharedData?.Devices != null && panelIndex < sharedData.Devices.Count)
            {
                device = sharedData.Devices[panelIndex];
                hasValidDevice = (device != null);
                
                // 🎯 디바이스 찾기 로그 제거 - 과도한 디버그 정보
                // sharedData.AddDeviceLog(0, $"🎯 Panel #{panelIndex}: Device 찾음", "DEBUG");
            }
            else
            {
                // 🎯 Device 없음 로그 제거 - 과도한 디버그 정보
                // sharedData?.AddDeviceLog(0, $"🔴 Panel #{panelIndex}: Device 없음", "DEBUG");
            }

            // UI 업데이트
            devicePanel.SuspendLayout();
            devicePanel.Controls.Clear();

            if (hasValidDevice && device != null)
            {
                // 실제 디바이스 있음 - 정상 표시
                CreateDeviceContent(devicePanel, device, panelIndex);
                // 🎯 컨텐츠 생성 로그 제거 - 과도한 디버그 정보
                // sharedData.AddDeviceLog(sharedData.GetIndexOfDevice(device), $"✅ Panel #{panelIndex}: 생성 완료", "DEBUG");
            }
            else
            {
                // 디바이스 없음 - Not Connected 슬롯 표시
                CreateNotConnectedSlot(devicePanel, panelIndex);
                // 🎯 Not Connected 슬롯 생성 로그 제거 - 과도한 디버그 정보
                // sharedData?.AddDeviceLog(0, $"✅ Panel #{panelIndex}: Not Connected Slot 생성 완료", "DEBUG");
            }

            devicePanel.ResumeLayout();
        }

        // 🔄 Manual Test 버튼 상태만 업데이트 (UI 전체 재생성 없이)
        private void UpdateManualTestButtonStates(Panel devicePanel, int deviceIndex, UnifiedDeviceData device)
        {
            // Manual Test 섹션 찾기
            //var foundPanel =
            //    devicePanels[deviceIndex].Controls[0].Controls[2];

            if (devicePanels[deviceIndex].Controls[0].Controls.Count > 2)
            {
                var foundPanel = devicePanels[deviceIndex].Controls[0].Controls[2];


                foreach (Control control in devicePanel.Controls)
                {
                    if (control is Panel cardContainer)
                    {
                        foreach (Control cardChild in cardContainer.Controls)
                        {
                            if (cardChild is Panel section && section.Location.Y > 400) // Manual Test 섹션 위치
                            {
                                // Manual Test 항목들 찾기
                                foreach (Control sectionChild in section.Controls)
                                {
                                    if (sectionChild is Panel testItem && sectionChild.Location.Y > 45) // 헤더 아래
                                    {
                                        // 버튼 찾기
                                        foreach (Control itemChild in testItem.Controls)
                                        {
                                            if (itemChild is Button button && button.Tag != null)
                                            {
                                                dynamic tag = button.Tag;
                                                int testIndex = tag.TestIndex;
                                            
                                                // 해당 테스트 찾기
                                                if (testIndex < device.PeripheralData.ManualTests.Count)
                                                {
                                                    var test = device.PeripheralData.ManualTests[testIndex];
                                                
                                                    // 버튼 상태 업데이트
                                                    button.Text = test.ButtonState;
                                                
                                                    // ButtonState에 따른 버튼 스타일 설정
                                                    switch (test.ButtonState)
                                                    {
                                                        case "RUNNING":
                                                            button.BackColor = Color.Transparent;
                                                            button.ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF);
                                                            button.TextAlign = ContentAlignment.TopCenter;
                                                            break;
                                                        case "PASS":
                                                            button.ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6); // 초록색
                                                            button.BackColor = Color.Transparent;
                                                            button.Enabled = true;
                                                            button.TextAlign = ContentAlignment.TopRight;
                                                            break;
                                                        case "FAIL":
                                                        case "ERROR":
                                                            button.ForeColor = Color.FromArgb(0xFF, 0x40, 0x40); // 빨간색
                                                            button.BackColor = Color.Transparent;
                                                            button.Enabled = true;
                                                            button.TextAlign = ContentAlignment.TopRight;
                                                            break;
                                                        default: // "RUN"
                                                            button.BackColor = Color.FromArgb(0x57, 0x57, 0x65); // #575765
                                                            button.ForeColor = Color.White;
                                                            button.Enabled = true;
                                                            button.TextAlign = ContentAlignment.TopCenter;
                                                            break;
                                                    }
                                                
                                                    button.Select();
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        // 🔴 Device Not Connected 슬롯 생성 (고정 4개 슬롯) - 높이 통일
        private void CreateNotConnectedSlot(Panel panel, int slotIndex)
        {
            // 슬롯별 기본 이름 - VD #01, #02, #03, #04 고정
            string[] slotNames = { "VD #01", "VD #02", "VD #03", "VD #04" };
            string deviceName = slotIndex < slotNames.Length ? slotNames[slotIndex] : $"VD #{slotIndex + 1}";

            // 🎯 카드 메인 컨테이너 - 고정 높이
            var cardContainer = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, DEVICE_HEIGHT),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                BorderStyle = BorderStyle.None
            };

            // 📱 카드 헤더 (디바이스 이름) - 고정 높이 50
            var cardHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, 50),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                BorderStyle = BorderStyle.None
            };

            var nameLabel = new Label
            {
                Text = deviceName,
                Location = new Point(20, 10),
                Size = new Size(300, 30),
                Font = new Font("Noto Sans KR", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            cardHeader.Controls.Add(nameLabel);
            cardContainer.Controls.Add(cardHeader);

            // 🔴 Device Not Connected 섹션 - 높이 통일
            var notConnectedSection = CreateNotConnectedSection();
            cardContainer.Controls.Add(notConnectedSection);

            // 📝 카드 푸터 제거 - Not Connected 상태에서는 로그창 없이 깔끔하게
            // var cardFooter = CreateNotConnectedFooter(slotIndex);
            // cardContainer.Controls.Add(cardFooter);

            panel.Controls.Add(cardContainer);
        }

        // 🔴 Device Not Connected 섹션 (2페이지, 3페이지와 동일)
        private Panel CreateNotConnectedSection()
        {
            var section = new Panel
            {
                Location = new Point(0, 50), // 헤더 다음
                Size = new Size(DEVICE_WIDTH, 695), // 2페이지, 3페이지와 동일
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                BorderStyle = BorderStyle.None
            };

            // 🚫 금지 아이콘
            var icon = new Label
            {
                Text = "🚫",
                Location = new Point(DEVICE_WIDTH / 2 - 25, 250), // 2페이지, 3페이지와 동일
                Size = new Size(50, 50),
                Font = new Font("Arial", 28F),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9), // #D9D9D9
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            // Device Not Connected 텍스트
            var text = new Label
            {
                Text = "Device\nNot Connected",
                Location = new Point(DEVICE_WIDTH / 2 - 120, 320), // 2페이지, 3페이지와 동일
                Size = new Size(240, 100),
                Font = new Font("Noto Sans KR", 22F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9), // #D9D9D9
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            section.Controls.Add(icon);
            section.Controls.Add(text);
            return section;
        }

        // 📝 Device Not Connected 슬롯의 푸터 (2페이지, 3페이지와 정확히 동일) - 로그창 제거
        private Panel CreateNotConnectedFooter(int slotIndex)
        {
            var footer = new Panel
            {
                Location = new Point(0, DEVICE_HEIGHT - 70), // 2페이지, 3페이지와 동일
                Size = new Size(DEVICE_WIDTH, 170), // 2페이지, 3페이지와 동일
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                BorderStyle = BorderStyle.FixedSingle
            };

            // 🎯 Not Connected 상태에서는 로그창 제거 (깔끔한 UI)
            // 로그창 없이 빈 푸터만 표시

            return footer;
        }

        // 🎨 디바이스 컨텐츠 생성 (CSS 카드 구조에 맞게)
        private void CreateDeviceContent(Panel panel, UnifiedDeviceData device, int deviceIndex)
        {
            // 🎯 카드 메인 컨테이너 (CSS의 card)
            var cardContainer = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, DEVICE_HEIGHT + 50),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                BorderStyle = BorderStyle.None
            };

            // 📱 카드 헤더 (CSS의 card header)
            var cardHeader = CreateCardHeader(device, deviceIndex);
            cardContainer.Controls.Add(cardHeader);

            // 📊 카드 본문 (연결 상태에 따라 다르게)
            if (!device.IsConnected)
            {
                // 🔴 Device Not Connected 표시 (CSS와 동일)
                var notConnectedSection = CreateNotConnectedSection();
                cardContainer.Controls.Add(notConnectedSection);
                
                notConnectedSection.Region = CreateRoundedRegion(new Rectangle(0, 0, notConnectedSection.Size.Width, notConnectedSection.Size.Height), 25);
                
                // 📝 Not Connected용 푸터 제거 - 깔끔한 UI를 위해 로그창 없음
                // var notConnectedFooter = CreateNotConnectedFooter(deviceIndex);
                // cardContainer.Controls.Add(notConnectedFooter);
            }
            else
            {
                // 📝 연결된 디바이스용 카드 푸터 (Save 버튼 있음)
                var cardFooter = CreateCardFooter(device, deviceIndex);
                cardFooter.Height -= 50; // 먼저 높이를 줄이고
                
                // 스크롤 영역 (Header~Footer 사이) - SaveResult와 맞닿을 때까지 확장
                int scrollY = cardHeader.Bottom;
                int scrollHeight = DEVICE_HEIGHT - cardHeader.Height - cardFooter.Height;
                int availableHeight = scrollHeight - 60; // 여백 40px 제외한 실제 사용 가능 높이
                
                // Auto Test 섹션 - 테스트 항목 개수에 따라 동적 크기 계산
                int autoTestHeight = CalculateSectionHeight(device.PeripheralData.AutoTests.Count);
                var autoTestSection = CreateAutoTestSection(device, deviceIndex);
                autoTestSection.Size = new Size(DEVICE_WIDTH - 40, autoTestHeight);
                autoTestSection.Location = new Point(20, 20); // 스크롤 패널 내부에서 여백 적용
                autoTestSection.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, autoTestHeight), 25);

                // Manual Test 섹션 - 테스트 항목 개수에 따라 동적 크기 계산
                int manualTestHeight = CalculateSectionHeight(device.PeripheralData.ManualTests.Count);
                var manualTestSection = CreateManualTestSection(device, deviceIndex);
                manualTestSection.Size = new Size(DEVICE_WIDTH - 40, manualTestHeight);
                manualTestSection.Location = new Point(20, autoTestSection.Bottom + 20); // 스크롤 패널 내부에서 여백 적용
                manualTestSection.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, manualTestHeight), 25);
                // scrollContentPanel의 실제 필요한 높이 계산 (section들 + 여백들)
                int actualContentHeight = autoTestHeight + manualTestHeight + 40; // 20 + 20 (상단, 중간 여백만) - 하단 여백 제거
                
                // Save Result 버튼 위치 직접 계산 (DEVICE_HEIGHT - 180에서 시작)
                int saveButtonY = DEVICE_HEIGHT - 180;
                int maxAllowedHeight = saveButtonY - scrollY - 10; // Save Result 버튼 위 10px까지
                
                // 실제 필요한 크기와 최대 허용 크기 중 작은 값 사용
                int finalContentHeight = Math.Min(actualContentHeight, maxAllowedHeight);
                
                var scrollContentPanel = new Panel
                {
                    Location = new Point(0, scrollY), // 스크롤 패널은 전체 너비 사용
                    Size = new Size(DEVICE_WIDTH, finalContentHeight),
                    AutoScroll = true,
                    BackColor = Color.Transparent,
                };
                scrollContentPanel.Controls.Add(autoTestSection);
                scrollContentPanel.Controls.Add(manualTestSection);

                // 🎯 더미 컨트롤 제거 - 직접 위치 계산 방식 사용

                // 🎯 스크롤바 숨김 (수동 스크롤만 가능, 자동 스크롤 포기)
                scrollContentPanel.AutoScroll = false;  
                scrollContentPanel.VerticalScroll.Maximum = 0;
                scrollContentPanel.VerticalScroll.Visible = false;
                scrollContentPanel.HorizontalScroll.Maximum = 0;
                scrollContentPanel.HorizontalScroll.Visible = false;
                scrollContentPanel.AutoScroll = true;

                cardContainer.Controls.Add(scrollContentPanel);
                cardContainer.Controls.Add(cardFooter);
            }


            panel.Controls.Add(cardContainer);
        }

        // 📱 카드 헤더 생성 (CSS의 card header)
        private Panel CreateCardHeader(UnifiedDeviceData device, int deviceIndex)
        {
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, 50), // 48 → 50으로 늘림
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                BorderStyle = BorderStyle.None
            };

            // 디바이스 이름 - Device ID 영역 확장에 맞춰 크기 조정
            var nameLabel = new Label
            {
                Text = device.Name,
                Location = new Point(20, 10),
                Size = new Size(185, 30), // 260 → 185로 줄여서 Device ID 공간 확보
                Font = new Font("Noto Sans KR", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Device ID 영역 - 2배로 확장 (75 → 150)
            var deviceIdLabel = new Label
            {
                Text = !string.IsNullOrEmpty(device.DeviceId) ? device.DeviceId : "Device ID", // DeviceId가 있으면 실제 값, 없으면 "Device ID"
                Location = new Point(215, 15), // 290 → 215로 왼쪽으로 이동 (75px 만큼)
                Size = new Size(150, 24), // 75 → 150으로 2배 확장
                Font = new Font("Noto Sans KR", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            header.Controls.Add(nameLabel);
            header.Controls.Add(deviceIdLabel);
            return header;
        }


        private Button startButton;
        //private Action<int>? onStartButtonEnableCallback;

        //public void SetStartButtonEnableCallbackToMqtt(Data.UnifiedMqttManager mqtt)
        //{
        //    Console.WriteLine("SetStartButtonEnableCallbackToMqtt");
        //    // 콜백 정의해서 UnifiedMqtt에 전달
        //    mqtt.RegisterStartButtonCallback((deviceIndex) =>
        //    {
        //        EnableStartButtonByDeviceIndex(deviceIndex);
        //    });
        //}

        //private void EnableStartButtonByDeviceIndex(int deviceIndex)
        //{
        //    // 이 함수에서 실제 버튼 활성화 로직 수행
        //    Console.WriteLine("EnableStartButtonByDeviceIndex");

        //    startButton.Enabled = true;
        //    //if (startButtons.ContainsKey(deviceIndex))
        //    //{
        //    //    startButtons[deviceIndex].Enabled = true;
        //    //}
        //}

        private async Task TaskExeucte()
        {
            await Task.Delay(10000);
            startButton.Enabled = true;

        }
        // 🤖 Auto Test 섹션 생성 - 동적 크기 (SaveResult까지 확장)
        private Panel CreateAutoTestSection(UnifiedDeviceData device, int deviceIndex)
        {
            var section = new Panel
            {
                // Location과 Size는 부모에서 동적으로 설정됨
                BackColor = Color.FromArgb(0x46, 0x46, 0x52), // #464652
                BorderStyle = BorderStyle.None,
            };

            // 섹션 헤더
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH - 30, 45), 
                BackColor = Color.FromArgb(0x46, 0x46, 0x52), // #464652
            };

            var title = new Label
            {
                Text = "Auto Test",
                Location = new Point(20, 8),
                Size = new Size(140, 30), // 160 → 140으로 줄여서 Start 버튼 공간 확보
                Font = new Font("Noto Sans KR", 15F, FontStyle.Bold), // 16F → 15F로 줄여서 잘림 방지
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Start/Started 버튼 - 클릭 가능하게 변경
            startButton = new Button
            {
                Location = new Point(235, 10), // DEVICE_WIDTH - 120 → 250으로 고정
                Size = new Size(85, 28), // 80 → 85로 늘려서 "Started" 텍스트 완전 표시
                Text = "Start", // 기본값은 "Start"
                Font = new Font("Noto Sans KR", 11F, FontStyle.Regular), // 12F → 11F로 줄여서 잘림 방지
                ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF), // #50CAFF
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                FlatStyle = FlatStyle.Flat,
                Tag = new { DeviceIndex = deviceIndex, IsAutoTest = true }
            };
            startButton.Click += AutoTestStartButton_Click;

            //startButton.Enabled = false;
            //TaskExeucte();

            //SetStartButtonEnableCallbackToMqtt(unifiedMqtt);

            header.Controls.Add(title);
            header.Controls.Add(startButton);
            section.Controls.Add(header);

            // 테스트 항목들
            int yPos = 55; 
            for (int i = 0; i < device.PeripheralData.AutoTests.Count; i++)
            {
                var test = device.PeripheralData.AutoTests[i];
                var testItem = CreateAutoTestItem(test, i, deviceIndex);
                testItem.Location = new Point(20, yPos);
                section.Controls.Add(testItem);
                yPos += 35; 
            }

            return section;
        }

        // 🤖 Auto Test 아이템 생성 - 더 넓은 공간 활용
        private Panel CreateAutoTestItem(UnifiedTestItem test, int testIndex, int deviceIndex)
        {
            var item = new Panel
            {
                Size = new Size(DEVICE_WIDTH - 70, 35), // 64 → 70으로 조정
                BackColor = Color.Transparent
            };

            // 테스트 이름 - 더 넓은 공간 활용 (포커스 가능하게 설정)
            var nameLabel = new Label
            {
                Text = $"{testIndex + 1}. {test.Name}",
                Location = new Point(0, 7), 
                Size = new Size(150, 22), // 200 → 180으로 줄여서 상태 표시 공간 더 확보
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular), // 13F → 12F로 줄여서 잘림 방지
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9), // #D9D9D9
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                TabStop = true, // 포커스 가능하게 설정
                Name = $"AutoTestLabel_{deviceIndex}_{testIndex}" // 디바이스 인덱스도 포함
            };

            // 상태 표시 - RUNNING을 위해 더 넓게
            var statusLabel = new Label
            {
                Text = test.Status.ToUpper(),
                Location = new Point(190, 7), // 210 → 190으로 조정 (더 넓은 공간)
                Size = new Size(120, 22), // 110 → 120으로 RUNNING 텍스트 완전 표시
                Font = new Font("Noto Sans KR", 11F, FontStyle.Bold), // 12F → 11F로 줄여서 잘림 방지
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            // 상태별 색상 (CSS에 맞게)
            switch (test.Status.ToUpper())
            {
                case "PASS":
                case "COMPLETED":
                    statusLabel.ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6); // #00FFA6
                    break;
                case "FAIL":
                case "ERROR":
                    statusLabel.ForeColor = Color.FromArgb(0xFF, 0x40, 0x40); // #FF4040
                    break;
                case "RUNNING":
                    statusLabel.ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF); // #50CAFF
                    statusLabel.Text = "RUNNING";
                    break;
                default:
                    statusLabel.ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9); // #D9D9D9
                    break;
            }

            item.Controls.Add(nameLabel);
            item.Controls.Add(statusLabel);
            return item;
        }

        // 🎯 Auto Test Start 버튼 클릭 이벤트 (완전 독립 실행)
        private async void AutoTestStartButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("AutoTestStartButton_Click");
            if (sender is Button button && button.Tag != null)
            {
                dynamic tag = button.Tag;
                int deviceIndex = tag.DeviceIndex;

                Console.WriteLine($"deviceIndex: {deviceIndex}");
                
                // 🚀 각 디바이스별로 완전히 독립적인 Task 실행 (다른 디바이스 차단 안함)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // 🎯 UI 스레드에서 버튼 상태 변경
                        this.Invoke(new Action(() =>
                        {
                            button.Text = "Started";
                            button.Enabled = false;
                            button.ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF); // #50CAFF 유지
                        }));

                                    // ✅ 디바이스 인덱스 매핑 수정 - deviceIndex로 직접 찾기
                        var device = sharedData.GetDeviceByIndex(deviceIndex);
                        Console.WriteLine($"AutoTest Count: {device?.PeripheralData.AutoTests.Count ?? 0}");



                       
                        if (device != null && device.PeripheralData.AutoTests.Count > 0)
                        {
                                        // 🚀 모든 테스트를 **병렬로** 실행 (각 테스트가 독립적)
                            var testTasks = new List<Task>();
                            
                            for (int i = 0; i < device.PeripheralData.AutoTests.Count; i++)
                            {
                                var test = device.PeripheralData.AutoTests[i];
                                Console.WriteLine($"AutoTest{i}: {test.Index}, {test.Name},{test.Status} ");

                                int testIndex = i; // 클로저를 위한 지역 변수

                                                // 🚀 각 테스트를 개별 Task로 병렬 실행
                                 //var testTask = Task.Run(async () =>
                                 {
                                     try
                                     {
                                        // 🎯 RUNNING 상태로 업데이트 (SharedData API 사용)
                                         sharedData.UpdateAutoTestStatus(deviceIndex, testIndex, "RUNNING");
                                         this.Invoke(new Action(() => UpdateAutoTestStatus(deviceIndex, testIndex, "RUNNING")));
                                         
                                        // 🎯 테스트 실행 시각화를 위한 딜레이 (1초)
                                         await Task.Delay(500);
                                         
                                         
                                        // 🧪 실제 테스트 함수 실행 (병렬로)
                                         FunctionTestResult testResult = await test.ExecuteTestAsync(unifiedMqtt, deviceIndex, test.Name);
                                        Console.WriteLine("✅ Received result: " + testResult.Result);

                                        // 🎯 테스트 완료 후 추가 딜레이 (결과 확인용)
                                        await Task.Delay(500);

                                        Console.WriteLine("===========================1111=============== params:" + test.Params);
                                        Console.WriteLine("===========================2222=============== result:" + testResult.Result);
                                        Console.WriteLine("===========================3333=============== Description:" + testResult.Description);
                                        // 결과에 따라 PASS/FAIL 자동 판정
                                        string result = testResult.Result ? "PASS" : "FAIL";
                                         
                                        // 📊 SharedData API를 통한 상태 업데이트 (이벤트 발생)
                                         sharedData.UpdateAutoTestStatus(deviceIndex, testIndex, result);
                                         
                                        // 🎯 테스트 완료 로그만 유지 (중요한 결과)
                                         sharedData.AddDeviceLog(device.Index, $"✅ Auto Test: {test.Name} → {result}", "INFO");
                                         
                                        // 🎯 결과 상태로 UI 업데이트 (UI 스레드에서)
                                         this.Invoke(new Action(() => UpdateAutoTestStatus(deviceIndex, testIndex, result)));
                                     }
                                     catch (Exception ex)
                                     {
                                        // 오류 발생 시 ERROR 상태로 설정 (SharedData API 사용)
                                         sharedData.UpdateAutoTestStatus(deviceIndex, testIndex, "ERROR");
                                         sharedData.AddDeviceLog(device.Index, $"❌ Auto Test 오류: {test.Name} → {ex.Message}", "ERROR");
                                         this.Invoke(new Action(() => UpdateAutoTestStatus(deviceIndex, testIndex, "ERROR")));
                                     }
                                 }
                                 //);
                                 
                                 //testTasks.Add(testTask);
                                 
                                 // 🎯 테스트 시작 간격을 1초로 늘림 (시각적 확인 가능)
                                 //await Task.Delay(1000);
                            }
                            
                            // 🚀 모든 테스트 완료 대기 (병렬 실행)
                            await Task.WhenAll(testTasks);
                            
                            // 🎯 완료 로그
                            sharedData.AddDeviceLog(deviceIndex, $"✅ Auto Test 완료", "SUCCESS");
                        }
                        /*
                        else
                        {
                            // 🎯 임시 디바이스 추가 처리
                            if (device == null)
                            {
                                sharedData.AddDevice($"VD #{deviceIndex + 1:D2}");
                                device = sharedData.GetDeviceByIndex(deviceIndex);
                            }
                            
                            // 임시 디바이스의 테스트도 병렬 실행
                            if (device != null && device.PeripheralData.AutoTests.Count > 0)
                            {
                                var testTasks = new List<Task>();
                                
                                for (int i = 0; i < device.PeripheralData.AutoTests.Count; i++)
                                {
                                    var test = device.PeripheralData.AutoTests[i];
                                    Console.WriteLine($"AutoTest{i}: {test.Index}, {test.Name},{test.Status} ");
                                    
                                    int testIndex = i; // 클로저를 위한 지역 변수
                                    
                                     var testTask = Task.Run(async () =>
                                     {
                                         try
                                         {
                                             // 🎯 RUNNING 상태로 업데이트 (SharedData API 사용)
                                             sharedData.UpdateAutoTestStatus(deviceIndex, testIndex, "RUNNING");
                                             this.Invoke(new Action(() => UpdateAutoTestStatus(deviceIndex, testIndex, "RUNNING")));
                                             
                                             // 🎯 테스트 실행 시각화를 위한 딜레이 (1초)
                                             await Task.Delay(1000);
                                             
                                             // 🧪 실제 테스트 함수 실행
                                             FunctionTestResult testResult = await test.ExecuteTestAsync(unifiedMqtt, deviceIndex, test.Name);
                                             
                                             // 🎯 테스트 완료 후 추가 딜레이 (결과 확인용)
                                             await Task.Delay(500);
                                             
                                             // 결과에 따라 PASS/FAIL 자동 판정
                                             string result = testResult.Result ? "PASS" : "FAIL";
                                             
                                             // 📊 SharedData API를 통한 상태 업데이트 (이벤트 발생)
                                             sharedData.UpdateAutoTestStatus(deviceIndex, testIndex, result);
                                             
                                             // 🎯 테스트 완료 로그만 유지
                                             sharedData.AddDeviceLog(device.Index, $"✅ Auto Test: {test.Name} → {result}", "INFO");
                                             
                                             // 🎯 결과 상태로 UI 업데이트
                                             this.Invoke(new Action(() => UpdateAutoTestStatus(deviceIndex, testIndex, result)));
                                         }
                                         catch (Exception ex)
                                         {
                                             sharedData.UpdateAutoTestStatus(deviceIndex, testIndex, "ERROR");
                                             sharedData.AddDeviceLog(device.Index, $"❌ Auto Test 오류: {test.Name} → {ex.Message}", "ERROR");
                                             this.Invoke(new Action(() => UpdateAutoTestStatus(deviceIndex, testIndex, "ERROR")));
                                         }
                                     });
                                     
                                     testTasks.Add(testTask);
                                     await Task.Delay(1000); // 시작 간격을 1초로 늘림
                                }
                                
                                await Task.WhenAll(testTasks);
                            }
                        }
                        */
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Auto Test 전체 실행 오류: {ex.Message}");
                        sharedData.AddDeviceLog(deviceIndex, $"❌ Auto Test 전체 실행 오류: {ex.Message}", "ERROR");
                    }
                    finally
                    {
                        // 🎯 테스트 완료 후 버튼 다시 활성화 (UI 스레드에서)
                        this.Invoke(new Action(() =>
                        {
                            button.Text = "Start";
                            button.Enabled = true;
                        }));
                    }
                });
            }
        }

        // 🎯 Auto Test 개별 상태 업데이트 (UI만)
        private void UpdateAutoTestStatus(int deviceIndex, int testIndex, string newStatus)
        {
            if (deviceIndex >= devicePanels.Count) return;

            // 🔍 상태 라벨 찾기 (변수명 수정: devicePanel → statusLabel)
            var statusLabel =
                devicePanels[deviceIndex].Controls[0].Controls[1].Controls[0].Controls[testIndex + 1].Controls[1];

        // 🎯 상태 텍스트 업데이트
            statusLabel.Text = newStatus.ToUpper();
                                                
            // 상태별 색상 (CSS에 맞게)
            switch (newStatus.ToUpper())
            {
                case "PASS":
                case "COMPLETED":
                    statusLabel.ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6); // #00FFA6
                    break;
                case "FAIL":
                case "ERROR":
                    statusLabel.ForeColor = Color.FromArgb(0xFF, 0x40, 0x40); // #FF4040
                    break;
                case "RUNNING":
                    statusLabel.ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF); // #50CAFF
                    break;
                default:
                    statusLabel.ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9); // #D9D9D9
                    break;
            }
            
            statusLabel.Select();
            
            // 🎯 가짜 컨트롤 포커스로 간단 스크롤 + 시각적 피드백
            if (deviceIndex >= 0 && deviceIndex < devicePanels.Count)
            {
                try
                {
                    // 현재 항목 찾기
                    var devicePanel = devicePanels[deviceIndex];
                    var cardContainer = devicePanel.Controls[0];
                    var scrollContentPanel = cardContainer.Controls[2] as Panel;
                    
                    if (scrollContentPanel != null)
                    {
                        // 시각적 피드백과 스크롤 처리
                        var autoTestSection = scrollContentPanel.Controls[0];
                        var autoTestItem = autoTestSection.Controls[testIndex + 1];
                        
                                                                           // 🎯 스크롤 포기 - 시각적 피드백만 사용
                        
                        // RUNNING 상태일 때 강화된 시각적 피드백
                        if (newStatus == "RUNNING")
                        {
                            // 🎯 Auto Test 섹션 헤더에 현재 실행 중인 테스트 표시
                            try
                            {
                                var headerTitle = autoTestSection.Controls[0].Controls.OfType<Label>().FirstOrDefault();
                                if (headerTitle != null)
                                {
                                    var device = sharedData.GetDeviceByIndex(deviceIndex);
                                    if (device != null && testIndex < device.PeripheralData.AutoTests.Count)
                                    {
                                        var testName = device.PeripheralData.AutoTests[testIndex].Name;
                                        headerTitle.Text = $"Auto Test - 🔄 실행중: {testName}";
                                        headerTitle.ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF); // 파란색으로 강조
                                    }
                                }
                            }
                            catch (Exception) { }
                            
                            // 모든 항목의 하이라이트 및 강조 제거 (이전 항목들)
                            for (int i = 1; i < autoTestSection.Controls.Count; i++)
                            {
                                var item = autoTestSection.Controls[i];
                                item.BackColor = Color.Transparent;
                                item.Region = null; // 테두리 제거
                                
                                // 텍스트 라벨을 기본 상태로 복구
                                var nameLabel = item.Controls.OfType<Label>().FirstOrDefault();
                                if (nameLabel != null)
                                {
                                    nameLabel.Font = new Font("Noto Sans KR", 12F, FontStyle.Regular);
                                    nameLabel.ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9);
                                    nameLabel.Text = nameLabel.Text.Replace("⚡ ", ""); // 아이콘 제거`
                                }
                            }
                            
                            // 🎯 현재 항목에 강화된 시각적 효과 적용
                            // 1. 강한 배경색과 테두리
                            autoTestItem.BackColor = Color.FromArgb(80, 0x50, 0xCA, 0xFF); // 더 진한 파란색
                            
                            // 2. 둥근 테두리와 그림자 효과
                            var borderRect = new Rectangle(0, 0, autoTestItem.Width, autoTestItem.Height);
                            autoTestItem.Region = CreateRoundedRegion(borderRect, 8);
                            
                            // 3. 텍스트 강조 (볼드 + 진행 아이콘)
                            var currentItemLabel = autoTestItem.Controls.OfType<Label>().FirstOrDefault();
                            if (currentItemLabel != null)
                            {
                                var device = sharedData.GetDeviceByIndex(deviceIndex);
                                if (device != null && testIndex < device.PeripheralData.AutoTests.Count)
                                {
                                    var testName = device.PeripheralData.AutoTests[testIndex].Name;
                                    currentItemLabel.Text = $"⚡ {testIndex + 1}. {testName}"; // 진행 아이콘 추가
                                    currentItemLabel.Font = new Font("Noto Sans KR", 12F, FontStyle.Bold); // 볼드 처리
                                    currentItemLabel.ForeColor = Color.White; // 흰색으로 강조
                                }
                            }
                            
                            // 4. 펄스 애니메이션 (1초 후 약간 연하게, 2초 후 다시 진하게 반복)
                            var pulseTimer = new System.Windows.Forms.Timer();
                            var pulseState = 0; // 0: 진함, 1: 연함
                            pulseTimer.Interval = 1000;
                            pulseTimer.Tick += (s, e) => {
                                if (autoTestItem.IsDisposed) 
                                {
                                    pulseTimer.Stop();
                                    pulseTimer.Dispose();
                                    return;
                                }
                                
                                // 펄스 효과: 진함 ↔ 연함 반복
                                if (pulseState == 0)
                                {
                                    autoTestItem.BackColor = Color.FromArgb(40, 0x50, 0xCA, 0xFF); // 연함
                                    pulseState = 1;
                                }
                                else
                                {
                                    autoTestItem.BackColor = Color.FromArgb(80, 0x50, 0xCA, 0xFF); // 진함
                                    pulseState = 0;
                                }
                            };
                            pulseTimer.Start();
                        }
                        else
                        {
                            // RUNNING이 아닌 상태일 때 헤더 제목 복구
                            try
                            {
                                var headerTitle = autoTestSection.Controls[0].Controls.OfType<Label>().FirstOrDefault();
                                if (headerTitle != null)
                                {
                                    headerTitle.Text = "Auto Test";
                                    headerTitle.ForeColor = Color.White; // 기본 흰색으로 복구
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception)
                {
                    // 실패 시 무시
                }
            }

        }

        // 🔧 Manual Test 섹션 생성 - 동적 크기 (SaveResult까지 확장)
        private Panel CreateManualTestSection(UnifiedDeviceData device, int deviceIndex)
        {
            var section = new Panel
            {
                // Location과 Size는 부모에서 동적으로 설정됨
                BackColor = Color.FromArgb(0x46, 0x46, 0x52), // #464652
                BorderStyle = BorderStyle.None,
            };

            // 섹션 헤더
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH - 30, 45), 
                BackColor = Color.FromArgb(0x46, 0x46, 0x52), // #464652
            };

            var title = new Label
            {
                Text = "Manual Test",
                Location = new Point(20, 8),
                Size = new Size(180, 30),
                Font = new Font("Noto Sans KR", 15F, FontStyle.Bold), // 16F → 15F로 줄여서 잘림 방지
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            header.Controls.Add(title);
            section.Controls.Add(header);

            // Manual Test 항목들
            int yPos = 55; 
            for (int i = 0; i < device.PeripheralData.ManualTests.Count; i++)
            {
                var test = device.PeripheralData.ManualTests[i];
                var testItem = CreateManualTestItem(test, i, deviceIndex);
                testItem.Location = new Point(20, yPos);
                section.Controls.Add(testItem);
                yPos += 35; 
            }

            return section;
        }

        // 🤖 Manual Test 아이템 생성 - RUN 버튼 위치 오른쪽으로 이동
        private Panel CreateManualTestItem(UnifiedTestItem test, int testIndex, int deviceIndex)
        {
            var item = new Panel
            {
                Size = new Size(DEVICE_WIDTH - 70, 35), // 64 → 70으로 조정
                BackColor = Color.Transparent
            };

            // 테스트 이름 - RUNNING 버튼을 위해 더 크기 조정
            var nameLabel = new Label
            {
                Text = $"{testIndex + 1}. {test.Name}",
                Location = new Point(0, 7), 
                Size = new Size(140, 22), // 160 → 140으로 더 줄여서 RUNNING 버튼 공간 확보
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular), // 13F → 12F로 줄여서 잘림 방지
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9), // #D9D9D9
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 버튼 - ButtonState에 따라 상태 설정
            var button = new Button
            {
                Location = new Point(205, 6), // 235 → 215로 조정 (더 넓은 버튼 공간)
                Size = new Size(100, 28), // 60 → 85로 늘려서 RUNNING 텍스트 완전 표시
                Text = test.ButtonState, // ButtonState 사용
                Font = new Font("Noto Sans KR", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.TopCenter,
                FlatStyle = FlatStyle.Flat,
                TabStop = false, // 포커스 이동 방지
                Tag = new { DeviceIndex = deviceIndex, TestIndex = testIndex, IsAutoTest = false },
                FlatAppearance = { BorderSize = 0 }
            };

            // ButtonState에 따른 버튼 스타일 설정
            switch (test.ButtonState)
            {
                case "RUNNING":
                    button.BackColor = Color.Transparent;   
                    button.ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF); // 
                    button.TextAlign = ContentAlignment.TopCenter;
                    break;
                case "PASS":
                    button.ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6); // 초록색
                    button.BackColor = Color.Transparent;
                    button.TextAlign = ContentAlignment.TopRight;
                    button.Enabled = true;
                    break;
                case "FAIL":
                case "ERROR":
                    button.ForeColor = Color.FromArgb(0xFF, 0x40, 0x40); // 빨간색
                    button.BackColor = Color.Transparent;
                    button.TextAlign = ContentAlignment.TopRight;
                    button.Enabled = true;
                    break;
                default: // "RUN"
                    button.BackColor = Color.FromArgb(0x57, 0x57, 0x65); // #575765
                    button.ForeColor = Color.White;
                    button.TextAlign = ContentAlignment.TopCenter;
                    button.Enabled = true;
                    break;
            }
            button.Click += TestButton_Click;
            //button.Enabled = false;

            item.Controls.Add(nameLabel);
            item.Controls.Add(button);
            return item;
        }

        // 🎯 테스트 버튼 클릭 이벤트 (실제 함수 실행)
        private async void TestButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("TestButton_Click");
            if (sender is Button button && button.Tag != null)
            {
                dynamic tag = button.Tag;
                int deviceIndex = tag.DeviceIndex;
                int testIndex = tag.TestIndex;
                bool isAutoTest = tag.IsAutoTest;

                // 🎯 버튼 클릭 로그 최소화 - 디버그 로그 제거
                // sharedData.AddDeviceLog(deviceIndex, $"🔍 RUN 버튼 클릭됨: DeviceIndex={deviceIndex}, TestIndex={testIndex}", "DEBUG");
                

                // ✅ PASS/FAIL 상태에서 클릭 시 즉시 RUN 상태로 복구
                if (button.Text == "PASS" || button.Text == "FAIL" || button.Text == "ERROR")
                {
                    // ButtonState를 RUN으로 변경
                    var device = sharedData.GetDeviceByIndex(deviceIndex);
                                    if (device != null && testIndex < device.PeripheralData.ManualTests.Count)
                {
                    var test = device.PeripheralData.ManualTests[testIndex];
                    
                    // 📊 SharedData API를 통한 상태 복구 (이벤트 발생)
                    sharedData.UpdateManualTestStatus(deviceIndex, testIndex, "RUN", "RUN");
                    
                    // 🎯 상태 복구 로그 제거 - 불필요한 로그
                    // sharedData.AddDeviceLog(deviceIndex, $"🔄 버튼 상태 복구: {button.Text} → RUN", "DEBUG");
                    
                    // 버튼 직접 업데이트 (UI 전체 새로고침 없이)
                    this.Invoke(new Action(() => UpdateButtonState(button, device.PeripheralData.ManualTests[testIndex])));
                }
                    return; // 테스트 실행하지 않고 상태만 복구
                }

                if (!isAutoTest && deviceIndex < sharedData.Devices.Count)
                {
                            // 🎯 디버그 로그 제거 - 과도한 로그 방지
                            // sharedData.AddDeviceLog(deviceIndex, $"🔍 디바이스 찾기 시도", "DEBUG");
                    
                            // ✅ 디바이스 인덱스 매핑 수정 - deviceIndex로 직접 찾기
                    var device = sharedData.GetDeviceByIndex(deviceIndex); // 이제 UI와 Data 모두 0-3 사용
                    
                    if (device != null)
                    {
                    // 🎯 디버그 로그 최소화 - 과도한 로그 제거
                    // sharedData.AddDeviceLog(device.Index, $"✅ 디바이스 찾음: {device.Name}", "DEBUG");
                        
                        if (testIndex < device.PeripheralData.ManualTests.Count)
                        {
                            var test = device.PeripheralData.ManualTests[testIndex];
                            Console.WriteLine($"ManualTest{testIndex}: {test.Index}, {test.Name},{test.Status} ");
                                        // 🎯 테스트 정보 로그 제거 - 불필요한 디버그 정보
                                        // sharedData.AddDeviceLog(device.Index, $"🔍 테스트 정보: {test.Name}", "DEBUG");

                            // 🎯 테스트 함수 실행 후 결과를 팝업에 표시
                            FunctionTestResult testResult = new FunctionTestResult();
                            
                            if (test.TestFunction != null)
                            {
                                try
                                {
                                    // RUNNING 상태로 변경 (SharedData API 사용)
                                    sharedData.UpdateManualTestStatus(deviceIndex, testIndex, "RUNNING", "RUNNING");
                                    this.Invoke(new Action(() => UpdateButtonState(button, device.PeripheralData.ManualTests[testIndex])));
                                    
                                    //🎯 테스트 실행 로그 제거 -결과만 중요
                                    //sharedData.AddDeviceLog(device.Index, $"🏃 테스트 실행 시작: {test.Name}", "INFO");

                                    // 🧪 실제 테스트 함수 실행
                                    testResult = await test.ExecuteTestAsync(unifiedMqtt, deviceIndex, test.Name);
                                    
                                    // 🎯 실행 완료 로그 제거 - 결과에서 확인 가능
                                    // sharedData.AddDeviceLog(device.Index, $"✅ 테스트 실행 완료: {test.Name}", "INFO");
                                }
                                catch (Exception ex)
                                {
                                    testResult = new FunctionTestResult { Description = $"테스트 실행 중 오류 발생:\n{ex.Message}", Result = false };
                                    sharedData.AddDeviceLog(device.Index, $"❌ 테스트 실행 오류: {ex.Message}", "ERROR");
                                }
                                
                                                // RUN 상태로 되돌리기 (팝업 표시 전)
                                sharedData.UpdateManualTestStatus(deviceIndex, testIndex, "RUN", "RUN");
                                this.Invoke(new Action(() => UpdateButtonState(button, device.PeripheralData.ManualTests[testIndex])));
                            }
                            else
                            {
                                testResult = new FunctionTestResult { Description = "테스트 함수가 설정되지 않았습니다.\n관리자에게 문의하세요.", Result = false };
                            }

                            Console.WriteLine("AAAAAAA");
                            Console.WriteLine("test.Params:"+ test.Params);
                            //Console.WriteLine("testResult:"+testResult);


                            //test.Params = @"C:\\temp\\pic01.png";

                            // 🎯 테스트 결과 입력 다이얼로그 표시
                            //testResult.Description = "AAAAAAA";
                            //testResult.Description = test.Params;
                            //testResult.Description = @"C:\\temp\\pic01.png";
                            using (var dialog = new TestResultDialog(test.Name, $"Device: {device.Name}", testResult.Description, test.Params))
                            {
                                var dialogResult = dialog.ShowDialog(this.FindForm());

                                if (dialogResult == DialogResult.OK && dialog.DialogConfirmed)
                                {
                                                    // 사용자가 선택한 결과 적용
                                    string result = dialog.TestResult ? "PASS" : "FAIL";

                                                     // ✅ 결과 상태로 변경 (SharedData API를 통한 업데이트와 이벤트 발생)
                                    sharedData.UpdateManualTestStatus(deviceIndex, testIndex, result, result);

                                                     // 🎯 사용자 선택 결과만 간단히 로그
                                    sharedData.AddDeviceLog(device.Index, $"✅ Manual Test: {test.Name} → {result}", "INFO");

                                                     // 버튼 직접 업데이트 (UI 전체 새로고침 없이)
                                    this.Invoke(new Action(() => UpdateButtonState(button, device.PeripheralData.ManualTests[testIndex])));
                                }
                                else
                                {
                                                    // 취소된 경우 RUN 상태로 유지 - 로그 제거
                                                    // sharedData.AddDeviceLog(device.Index, $"🔄 테스트 선택 취소됨: {test.Name}", "INFO");
                                }
                            }





                        }
                        else
                        {
                            // 🎯 오류 로그 최소화 - 중요한 오류만
                            sharedData.AddDeviceLog(device.Index, $"❌ 테스트 인덱스 오류: {testIndex}", "ERROR");
                        }
                    }
                    else
                    {
                        // 🎯 디바이스 찾기 실패는 중요한 오류
                        sharedData.AddDeviceLog(deviceIndex, $"❌ 디바이스 찾기 실패", "ERROR");
                    }
                }
                // 🎯 조건 불만족 로그 제거 - 너무 상세한 디버그 정보
                // else { sharedData.AddDeviceLog(1, $"❌ 조건 불만족", "ERROR"); }
            }
            // 🎯 버튼 정보 없음 로그 제거 - 너무 상세한 디버그 정보  
            // else { sharedData.AddDeviceLog(1, "❌ 버튼 정보 없음", "ERROR"); }
        }

        // 🔄 개별 버튼 상태만 업데이트 (UI 전체 새로고침 없이)
        private void UpdateButtonState(Button button, UnifiedTestItem test)
        {
            button.Text = test.ButtonState;

            // ButtonState에 따른 버튼 스타일 설정
            switch (test.ButtonState)
            {
                case "RUNNING":
                    button.BackColor = Color.Transparent;
                    button.ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF);
                    button.TextAlign = ContentAlignment.TopCenter;
                    // RUNNING 상태에서는 클릭/호버 색상을 투명하게 유지
                    break;
                case "PASS":
                    button.ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6); // 초록색
                    button.BackColor = Color.Transparent;
                    button.Enabled = true;
                    button.TextAlign = ContentAlignment.TopRight;
                    // PASS 상태에서도 색상 변화 방지
                    break;
                case "FAIL":
                case "ERROR":
                    button.ForeColor = Color.FromArgb(0xFF, 0x40, 0x40); // 빨간색
                    button.BackColor = Color.Transparent;
                    button.Enabled = true;
                    button.TextAlign = ContentAlignment.TopRight;
                    // FAIL 상태에서도 색상 변화 방지
                    break;
                                    default: // "RUN" here
                        button.BackColor = Color.FromArgb(0x57, 0x57, 0x65); // #575765
                    button.ForeColor = Color.White;
                    button.Enabled = true;
                    button.TextAlign = ContentAlignment.TopCenter;
                    button.FlatAppearance.BorderSize = 0;
                    button.FlatAppearance.BorderColor = Color.Yellow;
                    // RUN 상태에서는 약간 다른 색상 허용 (클릭 피드백용)
                    break;
            }
        }

        // 🔧 Save 버튼 클릭 이벤트
        private void SaveButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                dynamic tag = button.Tag;
                int deviceIndex = tag.DeviceIndex;

                var device = sharedData.GetDeviceByIndex(deviceIndex);
                if (device != null)
                {
                    sharedData.AddDeviceLog(device.Index, $"Save Button clicked: {device.Name} (Index: {deviceIndex})", "INFO");

                    //Peripheral Test 탭의 Save버튼
                    //Save버튼 누르면 1> 로컬PC에 json 파일로 저장, 2>Android 기기에 json파일로 저장, 3> DB로 저장, 
                    int ac = device.PeripheralData.AutoTests.Count;
                    for (int i = 0; i < ac; i++)
                    {
                        UnifiedTestItem ti = device.PeripheralData.AutoTests[i];
                        Console.WriteLine($"[A-Test] {deviceIndex}, {ti.Name}, {ti.Result}, {ti.Params}");
                    }

                    int mc = device.PeripheralData.ManualTests.Count;
                    for (int i = 0; i < ac; i++)
                    {
                        UnifiedTestItem ti = device.PeripheralData.ManualTests[i];
                        Console.WriteLine($"[M-Test] {deviceIndex}, {ti.Name}, {ti.Result}, {ti.Params}");
                    }

                    //Save

                    //APK삭제 
                    
                }
                else
                {
                    // Not Connected 상태 또는 디바이스 없음
                    sharedData.AddDeviceLog(0, $"💾 Save Button clicked: Slot #{deviceIndex + 1} (Not Connected)", "INFO");
                }
                
                // 연결 상태와 관계없이 콜백 호출
                OnSaveButtonClicked?.Invoke(deviceIndex);
            }
        }

        // 📝 카드 푸터 생성 - 버튼과 로그창 포함 (연결된 디바이스용) - 푸터 크기 확장
        private Panel CreateCardFooter(UnifiedDeviceData device, int deviceIndex)
        {
            var footer = new Panel
            {
                Location = new Point(0, DEVICE_HEIGHT - 180), // 고정 위치 (Save Result 버튼 위치 고정)
                Size = new Size(DEVICE_WIDTH, 280), 
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // #575765
                BorderStyle = BorderStyle.FixedSingle // 로그창 구분을 위한 테두리
            };

            // 🔧 Save Result 버튼 (20% 높이 증가, Auto Test 스타일 폰트)
            var actionButton = new Button
            {
                Location = new Point(15, 10),
                Size = new Size(DEVICE_WIDTH - 35, 42), // 35 → 42 (20% 증가)
                Text = "Save Result",
                Font = new Font("Noto Sans KR", 12F, FontStyle.Bold), // Auto Test 스타일로 변경
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D), // #18181D
                BackColor = Color.FromArgb(0xF3, 0x71, 0x21), // #F37121 (주황색)
                FlatStyle = FlatStyle.Flat,
                TabStop = false, // 포커스 이동 방지
                Tag = new { DeviceIndex = deviceIndex },
            };
            actionButton.FlatAppearance.BorderSize = 0;
            actionButton.FlatAppearance.BorderColor = Color.FromArgb(0xF3, 0x71, 0x21);
            
            actionButton.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 35, 42), 8);
            actionButton.Click += SaveButton_Click;

            // 로그창 - AutoTest 방식 Panel (Section + ScrollContainer) - 40픽셀 줄여서 하단 여백 확보
            var logPanel = new Panel
            {
                Location = new Point(15, 62),
                Size = new Size(DEVICE_WIDTH - 30, 160), // 200 → 160으로 40픽셀 줄임
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None,
                Name = $"LogPanel_{deviceIndex}"
            };

            // 🎯 더블버퍼링 적용
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, logPanel, new object[] { true });

            // 🎯 스크롤바 완전히 숨기고 가로 스크롤 완전 비활성화
            // 🎯 AutoTest scrollContentPanel과 완전히 동일한 스크롤바 설정!
            logPanel.AutoScroll = false;  
            logPanel.VerticalScroll.Maximum = 0;
            logPanel.VerticalScroll.Visible = false;
            logPanel.HorizontalScroll.Maximum = 0;
            logPanel.HorizontalScroll.Visible = false;
            logPanel.AutoScroll = true;

            // 초기 로그 메시지 추가 (AutoTest 방식)
            if (!device.IsConnected)
            {
                AddLogToPanel(logPanel, "[Step-1] Device Connecting ...", Color.FromArgb(0x80, 0x80, 0x80));
            }
            else
            {
                AddLogToPanel(logPanel, $"[Connected] {device.DeviceId}", Color.White);
                AddLogToPanel(logPanel, $"Auto: {device.PeripheralData.AutoTests.Count}, Manual: {device.PeripheralData.ManualTests.Count}", Color.FromArgb(0xD9, 0xD9, 0xD9));
            }

            footer.Controls.Add(actionButton);
            footer.Controls.Add(logPanel);
            return footer;
        }

        // 🎮 외부 API 메서드들 - SharedApplicationData 중심으로 단순화
        public void SetupBasicDevices()
        {
            // SharedApplicationData에서 기본 디바이스 설정
            sharedData.SetupBasicDevices();
            RefreshUI();
        }

        public void ClearAllDevices()
        {
            sharedData.ClearAllDevices();
            RefreshUI();
        }

        public IReadOnlyList<UnifiedDeviceData> GetAllDevices()
        {
            return sharedData.Devices;
        }

        // 🔄 기존 호환성 메서드들 (deprecated, SharedApplicationData가 자동 처리)
        public void OnDeviceConnectionChanged(int index, SharedDeviceData device) 
        {
            // SharedApplicationData가 자동으로 처리
        }
        
        public void OnDeviceDataChanged(int index, SharedDeviceData device) 
        {
            // SharedApplicationData가 자동으로 처리
        }
        
        public void OnDeviceAdded(SharedDeviceData device) 
        {
            // SharedApplicationData가 자동으로 처리
        }
        
        public void OnDeviceRemoved(int index) 
        {
            // SharedApplicationData가 자동으로 처리
        }
        
        public void RefreshFromDeviceManager() 
        {
            // SharedApplicationData가 자동으로 처리
        }

        // 🔍 기존 디바이스 카드 찾기
        private Panel? GetExistingDeviceCard(Panel devicePanel)
        {
            foreach (Control control in devicePanel.Controls)
            {
                if (control is Panel cardContainer)
                {
                    return cardContainer;
                }
            }
            return null;
        }
        


        // 📏 테스트 항목 개수에 따른 섹션 높이 계산
        private int CalculateSectionHeight(int testCount)
        {
            if (testCount == 0)
            {
                // 테스트가 없으면 헤더 + 여백만
                return 45 + 20; // 65px
            }
            
            // 실제 section 구조에 맞게 계산:
            // - 헤더: 0~45px
            // - 첫 번째 아이템: 55px에서 시작
            // - 마지막 아이템: 55 + (testCount-1) * 35 + 35 = 55 + testCount * 35에서 끝
            // - 하단 여백: 20px
            int lastItemEnd = 55 + (testCount * 35);
            return lastItemEnd + 20;
        }



        // 🎨 둥근 모서리 Region 생성 유틸리티
        private System.Drawing.Region CreateRoundedRegion(Rectangle rect, int radius)
        {
            System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.X + rect.Width - radius, rect.Y + rect.Height - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return new System.Drawing.Region(path);
        }

        // 📝 AutoTest 방식 로그 시스템 (Section + ScrollContainer)
        private void AddLogToPanel(Panel logPanel, string message, Color textColor)
        {
            // AutoTest 방식: 실제 로그 섹션 찾기
            Panel logSection = null;
            foreach (Control control in logPanel.Controls)
            {
                if (control is Panel section && control.Name == "LogSection")
                {
                    logSection = section;
                    break;
                }
            }
            
            // 로그 섹션이 없으면 생성 (AutoTest처럼)
            if (logSection == null)
            {
                logSection = new Panel
                {
                    Name = "LogSection",
                    Location = new Point(0, 0),
                    Size = new Size(logPanel.Width, 20), // 초기 높이
                    BackColor = Color.Transparent,
                    BorderStyle = BorderStyle.None
                };
                logPanel.Controls.Add(logSection);
            }

            // AutoTest 아이템 방식으로 로그 라벨 생성
            var logLabel = new Label
            {
                Text = message,
                Location = new Point(5, logSection.Controls.Count * 20), // AutoTest처럼 20px 간격
                Size = new Size(logSection.Width - 10, 18),
                Font = new Font("Noto Sans KR", 10F, FontStyle.Regular),
                ForeColor = textColor,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            logSection.Controls.Add(logLabel);
            
            // AutoTest처럼 섹션 높이를 동적 조정
            logSection.Size = new Size(logSection.Width, logSection.Controls.Count * 20);

            // 로그 제한 (성능 최적화)
            if (logSection.Controls.Count > 50)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (logSection.Controls.Count > 0)
                    {
                        logSection.Controls.RemoveAt(0);
                    }
                }
                
                // 위치 재조정
                for (int i = 0; i < logSection.Controls.Count; i++)
                {
                    logSection.Controls[i].Location = new Point(5, i * 20);
                }
                
                // 섹션 높이 재조정 (AutoTest처럼)
                logSection.Size = new Size(logSection.Width, logSection.Controls.Count * 20);
            }

            // 🎯 스크롤바 숨기기 꼼수: 먼저 크기 늘리고 나서 포커스!
            logLabel.TabStop = true;
            
            // 1. 원래 크기 저장
            var originalSize = logPanel.Size;
            
            // 2. 먼저 가로 사이즈를 엄청 크게 (스크롤바를 미리 화면 밖으로!)
            logPanel.Size = new Size(10000, originalSize.Height);
            
            // 3. 잠깐 기다렸다가 포커스 (크기 변경이 완전히 적용된 후)
            Application.DoEvents(); // UI 업데이트 강제 적용
            
            try
            {
                logLabel.Focus();
            }
            catch (Exception)
            {
                // 포커스 실패 시 무시
            }
            
            // 4. 즉시 원래 크기로 복구
            logPanel.Size = originalSize;
        }


        // 🔍 연결된 상태의 컨텐츠가 있는지 확인
        private bool HasConnectedContent(Panel cardContainer)
        {
            // Auto Test 섹션이나 Manual Test 섹션이 있으면 연결된 상태
            foreach (Control child in cardContainer.Controls)
            {
                if (child is Panel section && section.Location.Y > 50 && section.Location.Y < 500)
                {
                    // "Device Not Connected" 섹션이 아닌 다른 섹션이 있으면 연결된 상태
                    foreach (Control sectionChild in section.Controls)
                    {
                        if (sectionChild is Label label && label.Text.Contains("Auto Test"))
                        {
                            return true; // Auto Test 섹션이 있으면 연결된 상태
                        }
                    }
                }
            }
            return false;
        }
    }
} 