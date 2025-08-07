using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using AndroidDeviceVerificationTool.Design;
using AndroidDeviceVerificationTool.Data;

namespace AndroidDeviceVerificationTool.Controls
{
    /// <summary>
    /// 📝 IMEI/SN Write 컨트롤 - PeripheralTestControl과 동일한 구조
    /// 4개 디바이스 고정 레이아웃, SharedApplicationData 중심 제어
    /// </summary>
    public partial class ImeiSnWriteControl : UserControl
    {
        // 🌐 통합 데이터 관리자 (모든 컨트롤이 공유)
        private SharedApplicationData sharedData;

        // UI 컨테이너들
        private Panel mainPanel;
        private List<Panel> devicePanels = new List<Panel>();
        private bool isInitialized = false;

        // 🎨 1600x900 크기 최적화 상수들 (PeripheralTestControl과 동일)
        private const int DEVICE_WIDTH = 375;
        private const int DEVICE_HEIGHT = 815;
        private const int DEVICE_SPACING = 15;
        private const int START_MARGIN = 25;

        // 🎯 Form에서 구독할 수 있는 이벤트들
        public event Action<UnifiedDeviceData>? WriteButtonClicked;

        public ImeiSnWriteControl()
        {
            // 🎨 **더블버퍼링 활성화** - 깜빡임 완전 제거
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, false);
            
            // ✅ InitializeComponent() 완전 제거 - 디자이너 파일 사용 안함
            // InitializeComponent(); // 이 줄을 완전히 제거
            
            // 🌐 통합 데이터 관리자 연결
            sharedData = SharedApplicationData.Instance;
            
            // 🎨 Material Design 배경색 강제 설정
            this.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
            
            CreateMainUI();
            SubscribeToSharedDataEvents();
            
            isInitialized = true;
            RefreshUI();
        }

        // 🎨 메인 UI 생성 (PeripheralTestControl과 동일한 구조)
        private void CreateMainUI()
        {
            // 🎨 배경색 다시 한번 강제 설정
            this.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
            this.Size = new Size(1600, 875);

            // 메인 패널 - PeripheralTestControl과 동일하게 Dock.Fill + 더블버퍼링
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(47, 47, 55), // #2F2F37
                AutoScroll = false,
                HorizontalScroll = { Enabled = false, Visible = false },
                VerticalScroll = { Enabled = false, Visible = false }
            };
            
            // 🎨 **메인 패널에 더블버퍼링 적용**
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, mainPanel, new object[] { true });

            this.Controls.Add(mainPanel);

            // 4개 디바이스 패널 고정 생성
            CreateFixedDevicePanels();
            
            // 🎨 배경색 마지막 한번 더 설정
            this.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
        }   

        // 📱 4개 디바이스 패널 고정 생성 (PeripheralTestControl과 동일)
        private void CreateFixedDevicePanels()
        {
            devicePanels.Clear();
            mainPanel.Controls.Clear();

            for (int i = 0; i < 4; i++)
            {
                int xPos = START_MARGIN + (i * (DEVICE_WIDTH + DEVICE_SPACING));
                
                var devicePanel = new Panel
                {
                    Location = new Point(xPos, 10),
                    Size = new Size(DEVICE_WIDTH, DEVICE_HEIGHT),
                    BackColor = Color.FromArgb(47, 47, 55),
                    BorderStyle = BorderStyle.None,
                    AutoScroll = true,
                    HorizontalScroll = { Enabled = false, Visible = false },
                    Tag = i
                };

                // 🎨 **각 디바이스 패널에 더블버퍼링 적용**
                typeof(Panel).InvokeMember("DoubleBuffered",
                    BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                    null, devicePanel, new object[] { true });

                mainPanel.Controls.Add(devicePanel);
                devicePanels.Add(devicePanel);
            }
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
            
            // MicInWriteControl에서는 모델명을 Model 필드에 반영할 수 있음
            sharedData?.AddDeviceLog(0, $"🏷️ MicInWrite: 모델명 변경됨 - '{modelName}'", "INFO");
            
            // 연결된 디바이스들의 Model 필드를 업데이트
            for (int i = 0; i < sharedData.Devices.Count; i++)
            {
                var device = sharedData.Devices[i];
                if (device.IsConnected && !string.IsNullOrEmpty(modelName) && modelName != "N/A")
                {
                    device.IMEI_SN_Data.Model = modelName;
                    UpdateModelUI(i, modelName);
                }
            }
        }
        
        // 🏷️ Model UI 업데이트 헬퍼 메서드
        private void UpdateModelUI(int deviceIndex, string modelName)
        {
            try
            {
                if (deviceIndex >= 0 && deviceIndex < devicePanels.Count)
                {
                    var devicePanel = devicePanels[deviceIndex];
                    // Model TextBox 찾아서 업데이트
                    var modelTextBox = FindControlByName(devicePanel, $"ModelTextBox_{deviceIndex}") as TextBox;
                    if (modelTextBox != null)
                    {
                        modelTextBox.Text = modelName;
                    }
                }
            }
            catch (Exception ex)
            {
                sharedData?.AddDeviceLog(deviceIndex, $"❌ Model UI 업데이트 실패: {ex.Message}", "ERROR");
            }
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
                // sharedData.AddDeviceLog(index, $"🔥 MicInWrite: DeviceDataChanged 이벤트 호출됨!", "DEBUG");
                
                // 🎯 테스트 카운트만 조용히 업데이트 (UI 새로고침 없음)
                UpdateTestCounts(panelIndex, device);
                
                // 🎯 완전히 조용한 처리 - 로그도 생성하지 않음  
                // sharedData.AddDeviceLog(index, $"📊 MicInWrite: 테스트 카운트 업데이트 완료", "DEBUG");
            }
            // 오류 로그도 제거 - 너무 많은 로그 생성 방지
            // else { sharedData.AddDeviceLog(index, $"❌ 잘못된 panelIndex: {panelIndex}", "ERROR"); }
        }

        private void OnSharedDeviceConnectionChanged(int index, UnifiedDeviceData device)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSharedDeviceConnectionChanged(index, device)));
                return;
            }
            
            // ✅ **연결 상태 변경은 중요하므로 UI 새로고침 허용** (유일한 새로고침 허용 케이스)
            int panelIndex = index; // index - 1 제거
            if (panelIndex >= 0 && panelIndex < 4)
            {
                // 연결 상태 변경만 UI 새로고침 (중요한 구조적 변경)
                RefreshDevicePanel(panelIndex);
                
                // 로그도 최소화 - 연결 상태 변경 시에만
                // sharedData.AddDeviceLog(index, $"📝 MicInWrite: Device #{index} 연결 상태 변경", "INFO");
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
            // RefreshUI(); // 제거
            
            // 🎯 완전히 조용한 처리 - 로그도 생성하지 않음
            // sharedData.AddDeviceLog(device.Index, $"📝 MicInWrite: Device #{device.Index} 추가됨", "INFO");
        }

        private void OnSharedDeviceRemoved(int index)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnSharedDeviceRemoved(index)));
                return;
            }
            
            // 🚫 **디바이스 제거 시 UI 새로고침 완전 차단**
            // RefreshUI(); // 제거
            
            // 🎯 완전히 조용한 처리 - 로그도 생성하지 않음
            // sharedData.AddDeviceLog(index, $"📝 MicInWrite: Device #{index} 제거됨", "INFO");
        }

        // 🔄 전체 UI 새로고침
        public void RefreshUI()
        {
            if (!isInitialized) return;

            for (int i = 0; i < 4; i++)
            {
                RefreshDevicePanel(i);
            }
        }

        // 📱 개별 디바이스 패널 새로고침
        private void RefreshDevicePanel(int panelIndex)
        {
            if (panelIndex >= devicePanels.Count) return;

            var devicePanel = devicePanels[panelIndex];
            devicePanel.SuspendLayout();
            devicePanel.Controls.Clear();

            // ✅ 디바이스 인덱스 매핑 수정 - SharedData에서 실제 Index로 찾기
            UnifiedDeviceData device = null;
            
            // 먼저 정확한 Index로 찾기 (panelIndex는 0-3, device.Index는 0-3으로 통일)
            device = sharedData.GetDeviceByIndex(panelIndex);
            
            // 없으면 전체에서 순서대로 찾기 (fallback)
            if (device == null && panelIndex < sharedData.Devices.Count)
            {
                device = sharedData.Devices[panelIndex];
            }

            if (device == null)
            {
                // 디바이스가 없으면 "Device Not Connected" 표시
                CreateNotConnectedSlot(devicePanel, panelIndex);
            }
            else
            {
                // 디바이스가 있으면 실제 디바이스 카드 표시
                CreateDeviceContent(devicePanel, device, panelIndex);
            }

            devicePanel.ResumeLayout();
        }

        // 🔴 Device Not Connected 슬롯 생성
        private void CreateNotConnectedSlot(Panel panel, int slotIndex)
        {
            // 슬롯별 기본 이름
            string[] slotNames = { "VD #01", "VD #02", "VD #03", "VD #04" };
            string deviceName = slotIndex < slotNames.Length ? slotNames[slotIndex] : $"VD #{slotIndex + 1}";

            // 🎯 카드 메인 컨테이너
            var cardContainer = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, DEVICE_HEIGHT),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None
            };

            // 📱 카드 헤더
            var cardHeader = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, 50),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None
            };

            var nameLabel = new Label
            {
                Text = deviceName,
                Location = new Point(20, 10),
                Size = new Size(260, 30),
                Font = new Font("Noto Sans KR", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            cardHeader.Controls.Add(nameLabel);
            cardContainer.Controls.Add(cardHeader);

            // 🔴 Device Not Connected 섹션
            var notConnectedSection = CreateNotConnectedSection();
            cardContainer.Controls.Add(notConnectedSection);

            // 📝 카드 푸터
            var cardFooter = CreateNotConnectedFooter();
            cardContainer.Controls.Add(cardFooter);

            panel.Controls.Add(cardContainer);
        }

        // 🔴 Device Not Connected 섹션 - 푸터 확장으로 높이 조정
        private Panel CreateNotConnectedSection()
        {
            var section = new Panel
            {
                Location = new Point(0, 50),
                Size = new Size(DEVICE_WIDTH, 695), // 원래 크기로 복원
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None
            };

            // 🚫 금지 아이콘
            var icon = new Label
            {
                Text = "🚫",
                Location = new Point(DEVICE_WIDTH / 2 - 25, 250),
                Size = new Size(50, 50),
                Font = new Font("Arial", 28F),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            // Device Not Connected 텍스트
            var text = new Label
            {
                Text = "Device\nNot Connected",
                Location = new Point(DEVICE_WIDTH / 2 - 120, 320),
                Size = new Size(240, 100),
                Font = new Font("Noto Sans KR", 22F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            section.Controls.Add(icon);
            section.Controls.Add(text);
            return section;
        }

        // 📝 Device Not Connected 푸터 - 1페이지와 완전히 동일
        private Panel CreateNotConnectedFooter()
        {
            var footer = new Panel
            {
                Location = new Point(0, DEVICE_HEIGHT - 180), // 1페이지와 완전히 동일
                Size = new Size(DEVICE_WIDTH, 280), // 1페이지와 완전히 동일
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 로그창 - AutoTest 방식 Panel (Section + ScrollContainer) - 1페이지와 완전히 동일
            var logPanel = new Panel
            {
                Location = new Point(15, 62),
                Size = new Size(DEVICE_WIDTH - 30, 160), // 1페이지와 정확히 동일한 160px
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None,
                Name = "LogPanel_NotConnected"
            };

            // 🎯 더블버퍼링 적용
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, logPanel, new object[] { true });

            // 🎯 스크롤바 완전히 숨기고 가로 스크롤 완전 비활성화
            // 🎯 AutoTest scrollContentPanel과 완전히 동일한 스크롤바 설정! (1페이지와 완전히 동일)
            logPanel.AutoScroll = false;  
            logPanel.VerticalScroll.Maximum = 0;
            logPanel.VerticalScroll.Visible = false;
            logPanel.HorizontalScroll.Maximum = 0;
            logPanel.HorizontalScroll.Visible = false;
            logPanel.AutoScroll = true;

            // 초기 로그 메시지 추가 (1페이지와 완전히 동일)
            AddLogToPanel(logPanel, "[Step-1] Device Connecting ...", Color.FromArgb(0x80, 0x80, 0x80));

            footer.Controls.Add(logPanel);
            return footer;
        }

        // 🎨 디바이스 컨텐츠 생성
        private void CreateDeviceContent(Panel panel, UnifiedDeviceData device, int deviceIndex)
        {
            // 🎯 카드 메인 컨테이너
            var cardContainer = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, DEVICE_HEIGHT + 50),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None
            };

            // 📱 카드 헤더
            var cardHeader = CreateCardHeader(device, deviceIndex);
            cardContainer.Controls.Add(cardHeader);

            if (!device.IsConnected)
            {
                // 🔴 Device Not Connected 표시
                var notConnectedSection = CreateNotConnectedSection();
                cardContainer.Controls.Add(notConnectedSection);
            }
            else
            {
                // 📝 연결된 디바이스용 카드 푸터
                var cardFooter = CreateCardFooter(device, deviceIndex);
                
                // Auto Test 섹션 - 1페이지와 시작 높이 맞추기, 고정 레이아웃
                var autoTestSection = CreateAutoTestSection(device, deviceIndex);
                autoTestSection.Size = new Size(DEVICE_WIDTH - 40, 140); // 적당한 크기
                autoTestSection.Location = new Point(20, cardHeader.Bottom + 20); // 1페이지와 동일한 시작 높이
                // autoTestSection.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 140), 25);

                // Manual Test 섹션 - 적당한 간격과 크기
                var manualTestSection = CreateManualTestSection(device, deviceIndex);
                manualTestSection.Size = new Size(DEVICE_WIDTH - 40, 140); // Auto Test와 동일한 크기
                manualTestSection.Location = new Point(20, autoTestSection.Bottom + 20); // 적당한 간격
                manualTestSection.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 140), 25);

                // IMEI/SN Write 섹션 - 여유롭게 크게 만들기
                var imeiSnSection = CreateImeiSnWriteSection(device, deviceIndex);
                imeiSnSection.Size = new Size(DEVICE_WIDTH - 40, 200); // 크게 확장하여 여유롭게
                imeiSnSection.Location = new Point(20, manualTestSection.Bottom + 20); // 적당한 간격
                imeiSnSection.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 200), 25);

                // 🎯 스크롤 없는 고정 레이아웃 - 직접 카드에 추가
                cardContainer.Controls.Add(autoTestSection);
                cardContainer.Controls.Add(manualTestSection);
                cardContainer.Controls.Add(imeiSnSection);
                cardContainer.Controls.Add(cardFooter);
            }

            panel.Controls.Add(cardContainer);
        }

        // 📱 카드 헤더 생성
        private Panel CreateCardHeader(UnifiedDeviceData device, int deviceIndex)
        {
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH, 50),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None
            };

            var nameLabel = new Label
            {
                Text = device.Name,
                Location = new Point(20, 10),
                Size = new Size(185, 30),
                Font = new Font("Noto Sans KR", 16F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 항상 Device ID 영역 표시
            var deviceIdLabel = new Label
            {
                Text = !string.IsNullOrEmpty(device.DeviceId) ? device.DeviceId : "Device ID", // DeviceId가 있으면 실제 값, 없으면 "Device ID"
                Location = new Point(215, 15), // 290 → 215로 왼쪽으로 이동 (75px 만큼)
                Size = new Size(150, 24), // 75 → 150으로 2배 확장Size = new Size(150, 24), // 75 → 150으로 2배 확장
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



        // 🤖 Auto Test 섹션 생성 (Pass/Fail count만 표시)
        private Panel CreateAutoTestSection(UnifiedDeviceData device, int deviceIndex)
        {
            var section = new Panel
            {
                // Location은 부모에서 설정됨 (Page 1과 동일하게)
                Size = new Size(DEVICE_WIDTH - 40, 140), // 표준화된 너비
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                BorderStyle = BorderStyle.None,
            };
            
            // 🎨 둥근 모서리 적용
            section.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 140), 25);

            // 섹션 헤더
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH - 40, 45), // 표준화된 너비
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
            };

            var title = new Label
            {
                Text = "Auto Test",
                Location = new Point(25, 10), // 20,8 → 25,10으로 여백 증가
                Size = new Size(160, 32), // 140,30 → 160,32로 확장
                Font = new Font("Noto Sans KR", 16F, FontStyle.Bold), // 15F → 16F로 복원
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            header.Controls.Add(title);
            section.Controls.Add(header);

            // TestCount 정보만 (Pass/Fail 2줄)
            CreateTestCountInfo(section, device);

            return section;
        }



        // 🔧 Manual Test 섹션 생성 (Pass/Fail count만 표시)
        private Panel CreateManualTestSection(UnifiedDeviceData device, int deviceIndex)
        {
            var section = new Panel
            {
                // Location은 부모에서 설정됨 (Page 1과 동일하게)
                Size = new Size(DEVICE_WIDTH - 40, 140), // 표준화된 너비
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                BorderStyle = BorderStyle.None
            };
            
            // 🎨 둥근 모서리 적용
            section.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 140), 25);

            // 섹션 헤더
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH - 40, 45), // 표준화된 너비
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
            };

            var title = new Label
            {
                Text = "Manual Test",
                Location = new Point(25, 10), // 20,8 → 25,10으로 여백 증가 (Auto Test와 동일)
                Size = new Size(180, 32), // 200,30 → 180,32로 조정 (Auto Test와 비슷하게)
                Font = new Font("Noto Sans KR", 16F, FontStyle.Bold), // 15F → 16F로 복원 (Auto Test와 동일)
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            header.Controls.Add(title);
            section.Controls.Add(header);

            // Pass/Fail 카운트 정보 (Auto Test와 동일하게)
            CreateTestCountInfo(section, device, true);

            return section;
        }

        // 🤖 Manual Test 아이템 생성 - 1페이지와 완전히 동일
        private Panel CreateManualTestItem(UnifiedTestItem test, int testIndex, int deviceIndex)
        {
            var item = new Panel
            {
                Size = new Size(DEVICE_WIDTH - 70, 35), // 1페이지와 동일
                BackColor = Color.Transparent
            };

            // 테스트 이름 - 1페이지와 동일
            var nameLabel = new Label
            {
                Text = $"{testIndex + 1}. {test.Name}",
                Location = new Point(0, 7), 
                Size = new Size(140, 22), // 1페이지와 동일
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular), // 1페이지와 동일
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9), // #D9D9D9
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // 버튼 - 1페이지와 동일
            var button = new Button
            {
                Location = new Point(150, 5), // 1페이지와 동일
                Size = new Size(50, 25), // 1페이지와 동일
                Text = "RUN", // 기본값
                Font = new Font("Noto Sans KR", 10F, FontStyle.Bold), // 1페이지와 동일
                BackColor = Color.FromArgb(0x57, 0x57, 0x65), // 1페이지와 동일
                ForeColor = Color.FromArgb(0x50, 0xCA, 0xFF), // 1페이지와 동일
                FlatStyle = FlatStyle.Flat,
                Tag = new { Test = test, TestIndex = testIndex, DeviceIndex = deviceIndex }
            };
            button.FlatAppearance.BorderSize = 0;
            // button.Click += TestButton_Click; // 2페이지에서는 버튼 비활성화

            // 상태 표시 - 1페이지와 동일
            var statusLabel = new Label
            {
                Text = test.Status.ToUpper(),
                Location = new Point(210, 7), // 1페이지와 동일
                Size = new Size(100, 22), // 1페이지와 동일
                Font = new Font("Noto Sans KR", 11F, FontStyle.Bold), // 1페이지와 동일
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            // 상태별 색상 (1페이지와 완전히 동일)
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
                    break;
                default:
                    statusLabel.ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9); // #D9D9D9
                    break;
            }

            item.Controls.Add(nameLabel);
            item.Controls.Add(button);
            item.Controls.Add(statusLabel);
            return item;
        }

        // 📊 테스트 카운트 정보 생성 (Pass/Fail 2줄, 140px 높이에 맞는 여백)
        private void CreateTestCountInfo(Panel section, UnifiedDeviceData device, bool isManualTest = false)
        {
            var tests = isManualTest ? device.PeripheralData.ManualTests : device.PeripheralData.AutoTests;
            
            // ✅ Pass/Fail 카운트 실시간 계산 (대소문자 구분 없이)
            int passCount = tests.Count(t => 
                t.Status.Equals("PASS", StringComparison.OrdinalIgnoreCase) || 
                t.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase));
                
            int failCount = tests.Count(t => 
                t.Status.Equals("FAIL", StringComparison.OrdinalIgnoreCase) || 
                t.Status.Equals("ERROR", StringComparison.OrdinalIgnoreCase));

            // 📊 디버그: 카운트 계산 로그
            string testType = isManualTest ? "Manual" : "Auto";
            sharedData.AddDeviceLog(device.Index, $"📊 {testType} Test Count for Device #{device.Index}: Pass={passCount}, Fail={failCount} (Total={tests.Count})", "DEBUG");

            // 1줄: Pass count (녹색) - 140px 높이에 맞는 적절한 배치
            var passLabel = new Label
            {
                Text = "Pass count:",
                Location = new Point(30, 65), // 35,70 → 30,65로 조정
                Size = new Size(150, 26), // 155,28 → 150,26으로 축소
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular), // 15F → 14F로 축소
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent
            };

            var passValue = new Label
            {
                Text = passCount.ToString(),
                Location = new Point(280, 65), // 285,70 → 280,65로 조정
                Size = new Size(38, 26), // 40,28 → 38,26으로 축소
                Font = new Font("Noto Sans KR", 14F, FontStyle.Bold), // 15F → 14F로 축소
                ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6), // 초록색
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Name = isManualTest ? "ManualPassCount" : "AutoPassCount" // 이름으로 찾기 위한 Name 설정
            };

            // 2줄: Fail count (빨간색) - 140px 높이에 맞는 적절한 배치
            var failLabel = new Label
            {
                Text = "Fail count:",
                Location = new Point(30, 95), // 35,105 → 30,95로 조정
                Size = new Size(150, 26), // 155,28 → 150,26으로 축소
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular), // 15F → 14F로 축소
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent
            };

            var failValue = new Label
            {
                Text = failCount.ToString(),
                Location = new Point(280, 95), // 285,105 → 280,95로 조정
                Size = new Size(38, 26), // 40,28 → 38,26으로 축소
                Font = new Font("Noto Sans KR", 14F, FontStyle.Bold), // 15F → 14F로 축소
                ForeColor = Color.FromArgb(0xFF, 0x40, 0x40), // 빨간색
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleRight,
                Name = isManualTest ? "ManualFailCount" : "AutoFailCount" // 이름으로 찾기 위한 Name 설정
            };

            section.Controls.Add(passLabel);
            section.Controls.Add(passValue);
            section.Controls.Add(failLabel);
            section.Controls.Add(failValue);
        }

        // 🎯 IMEI/SN Write 섹션 생성 - 여유로운 크기
        private Panel CreateImeiSnWriteSection(UnifiedDeviceData device, int deviceIndex)
        {
            var section = new Panel
            {
                // Location은 부모에서 설정됨 (Page 1과 동일하게)
                Size = new Size(DEVICE_WIDTH - 40, 200), // 여유로운 크기로 확장
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                BorderStyle = BorderStyle.None
            };
            
            // 🎨 둥근 모서리 적용
            section.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 200), 25);

            // 섹션 헤더
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DEVICE_WIDTH - 40, 45), // 다른 섹션과 동일한 45px
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
            };

            var title = new Label
            {
                Text = "IMEI/SN",
                Location = new Point(25, 10), // 20,8 → 25,10으로 여백 증가 (다른 섹션과 동일)
                Size = new Size(140, 32), // 120,25 → 140,32로 확장 (다른 섹션과 비슷하게)
                Font = new Font("Noto Sans KR", 16F, FontStyle.Bold), // 14F → 16F로 복원 (다른 섹션과 동일)
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft
            };

            header.Controls.Add(title);
            section.Controls.Add(header);

            // IMEI/SN 입력 필드들
            CreateImeiSnFields(section, device, deviceIndex);

            return section;
        }

        // 📝 IMEI/SN 입력 필드들 생성 - 여유로운 200px 레이아웃
        private void CreateImeiSnFields(Panel section, UnifiedDeviceData device, int deviceIndex)
        {
            const int ELEMENT_HEIGHT = 32;  // 높이 더 증가 (30 → 32)
            const int LABEL_WIDTH = 75;     // 라벨 너비 더 증가 (70 → 75)
            const int TEXTBOX_WIDTH = 200;  // 텍스트박스 너비 더 확장 (195 → 200)
            const int LEFT_MARGIN = 30;     // 좌측 여백 더 증가 (28 → 30)
            const int ELEMENT_SPACING = 40; // 줄간격 더 증가 (35 → 40)

            int yPos = 55; // 시작 위치 더 증가 (50 → 55)

            // IMEI 라벨 - 200px 높이에 맞는 여유로운 크기
            var imeiLabel = new Label
            {
                Text = "IMEI:",
                Location = new Point(LEFT_MARGIN, yPos + 2),
                Size = new Size(LABEL_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 13F, FontStyle.Regular), // 12F → 13F로 증가
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                TabStop = false
            };

            // IMEI 텍스트 박스 - 200px 높이에 맞는 여유로운 크기
            var imeiTextBox = new TextBox
            {
                Location = new Point(LEFT_MARGIN + LABEL_WIDTH + 10, yPos), // 여백 증가 (8 → 10)
                Size = new Size(TEXTBOX_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 13F, FontStyle.Regular), // 12F → 13F로 증가
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BorderStyle = BorderStyle.FixedSingle,
                Text = device.IMEI_SN_Data.IMEI,
                TextAlign = HorizontalAlignment.Left,
                TabStop = false
            };
            imeiTextBox.TextChanged += (s, e) => UpdateDeviceImeiData(device, imeiTextBox.Text);

            section.Controls.Add(imeiLabel);
            section.Controls.Add(imeiTextBox);

            yPos += ELEMENT_SPACING;

            // Serial 라벨 - 200px 높이에 맞는 여유로운 크기
            var snLabel = new Label
            {
                Text = "Serial:",
                Location = new Point(LEFT_MARGIN, yPos + 2),
                Size = new Size(LABEL_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 13F, FontStyle.Regular), // 12F → 13F로 증가
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                TabStop = false
            };

            // Serial 텍스트 박스 - 200px 높이에 맞는 여유로운 크기
            var snTextBox = new TextBox
            {
                Location = new Point(LEFT_MARGIN + LABEL_WIDTH + 10, yPos), // 여백 증가 (8 → 10)
                Size = new Size(TEXTBOX_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 13F, FontStyle.Regular), // 12F → 13F로 증가
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BorderStyle = BorderStyle.FixedSingle,
                Text = device.IMEI_SN_Data.SerialNumber,
                TextAlign = HorizontalAlignment.Left,
                TabStop = false
            };
            snTextBox.TextChanged += (s, e) => UpdateDeviceSnData(device, snTextBox.Text);

            section.Controls.Add(snLabel);
            section.Controls.Add(snTextBox);

            yPos += ELEMENT_SPACING + 10; // Write 버튼을 위한 여유로운 간격 (더 아래로)

            // Write 버튼 - 200px 높이에 맞는 여유로운 크기
            var writeButton = new Button
            {
                Location = new Point(LEFT_MARGIN, yPos),
                Size = new Size(280, 38), // 버튼 크기 더 확장 (270,35 → 280,38)
                Text = "Write",
                Font = new Font("Noto Sans KR", 13F, FontStyle.Bold), // 폰트 크기 증가 (12F → 13F)
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter
            };
            writeButton.FlatAppearance.BorderSize = 0;
            writeButton.Click += (s, e) => WriteButton_Click(device);
            writeButton.Region = CreateRoundedRegion(new Rectangle(0, 0, 280, 38), 12); // 둥근 모서리 더 크게

            section.Controls.Add(writeButton);
        }

        // 📝 카드 푸터 생성 - 1페이지와 완전히 동일
        private Panel CreateCardFooter(UnifiedDeviceData device, int deviceIndex)
        {
            var footer = new Panel
            {
                Location = new Point(0, DEVICE_HEIGHT - 180), // 1페이지와 완전히 동일
                Size = new Size(DEVICE_WIDTH, 280), // 1페이지와 완전히 동일
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
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
                Tag = new { DeviceIndex = deviceIndex }
            };
            actionButton.FlatAppearance.BorderSize = 0;
            actionButton.FlatAppearance.BorderColor = Color.FromArgb(0xF3, 0x71, 0x21);
            actionButton.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 35, 42), 8);
            actionButton.Click += SaveResultButton_Click;

            // 로그창 - AutoTest 방식 Panel (Section + ScrollContainer) - 1페이지와 완전히 동일
            var logPanel = new Panel
            {
                Location = new Point(15, 62),
                Size = new Size(DEVICE_WIDTH - 30, 160), // 1페이지와 정확히 동일한 160px
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None,
                Name = $"LogPanel_{deviceIndex}"
            };

            // 🎯 더블버퍼링 적용
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, logPanel, new object[] { true });

            // 🎯 스크롤바 완전히 숨기고 가로 스크롤 완전 비활성화
            // 🎯 AutoTest scrollContentPanel과 완전히 동일한 스크롤바 설정! (1페이지와 완전히 동일)
            logPanel.AutoScroll = false;  
            logPanel.VerticalScroll.Maximum = 0;
            logPanel.VerticalScroll.Visible = false;
            logPanel.HorizontalScroll.Maximum = 0;
            logPanel.HorizontalScroll.Visible = false;
            logPanel.AutoScroll = true;

            // 초기 로그 메시지 추가 (1페이지와 완전히 동일한 방식)
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

        // 🎯 테스트 카운트만 업데이트 (전체 UI 새로고침 없이)
        private void UpdateTestCounts(int panelIndex, UnifiedDeviceData device)
        {
            if (panelIndex >= 0 && panelIndex < 4)
            {
                var devicePanel = devicePanels[panelIndex];
                if (devicePanel != null)
                {
                    // 📊 새로운 카운팅 API 사용
                    var testCounts = sharedData.GetAllTestCounts(panelIndex);
                    
                    // Auto Test 카운트 업데이트
                    UpdateTestCountLabels(devicePanel, false, testCounts.AutoPassCount, testCounts.AutoFailCount);
                    
                    // Manual Test 카운트 업데이트  
                    UpdateTestCountLabels(devicePanel, true, testCounts.ManualPassCount, testCounts.ManualFailCount);
                    
                    // 📊 디버그: API 사용 결과 로그
                    sharedData.AddDeviceLog(panelIndex, 
                        $"AutoTest (Pass={testCounts.AutoPassCount}, Fail={testCounts.AutoFailCount}), Manual(Pass={testCounts.ManualPassCount}, Fail={testCounts.ManualFailCount})", "INFO");
                }
            }
        }

        private void UpdateDeviceTestCounts(Panel devicePanel, UnifiedDeviceData device)
        {
            // 📊 새로운 카운팅 API 사용으로 간소화
            var testCounts = sharedData.GetAllTestCounts(device.Index);
            
            // sharedData.AddDeviceLog(device.Index, 
            //     $"📊 API를 통한 카운트 조회: Auto(Pass={testCounts.AutoPassCount}, Fail={testCounts.AutoFailCount}), Manual(Pass={testCounts.ManualPassCount}, Fail={testCounts.ManualFailCount})", "DEBUG");

            // Auto Test 카운트 업데이트
            UpdateTestCountLabels(devicePanel, false, testCounts.AutoPassCount, testCounts.AutoFailCount);
            
            // Manual Test 카운트 업데이트
            UpdateTestCountLabels(devicePanel, true, testCounts.ManualPassCount, testCounts.ManualFailCount);
        }

        private void UpdateSectionCounts(Control section, UnifiedDeviceData device, bool isManualTest)
        {
            // 📊 새로운 카운팅 API 사용
            var testCounts = sharedData.GetAllTestCounts(device.Index);
            
            int passCount = isManualTest ? testCounts.ManualPassCount : testCounts.AutoPassCount;
            int failCount = isManualTest ? testCounts.ManualFailCount : testCounts.AutoFailCount;

            // 🎯 Name으로 정확하게 라벨 찾아서 업데이트
            string passCountName = isManualTest ? "ManualPassCount" : "AutoPassCount";
            string failCountName = isManualTest ? "ManualFailCount" : "AutoFailCount";
            
            // Pass count 라벨 찾기
            var passLabel = FindControlByName(section, passCountName) as Label;
            if (passLabel != null)
            {
                passLabel.Text = passCount.ToString();
                passLabel.ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6); // 초록색
            }
            
            // Fail count 라벨 찾기
            var failLabel = FindControlByName(section, failCountName) as Label;
            if (failLabel != null)
            {
                failLabel.Text = failCount.ToString();
                failLabel.ForeColor = Color.FromArgb(0xFF, 0x40, 0x40); // 빨간색
            }

            // 📊 디버그: 카운트 업데이트 로그
            string testType = isManualTest ? "Manual" : "Auto";
            sharedData.AddDeviceLog(device.Index, $"🔄 {testType} Test Count 업데이트: Pass={passCount}, Fail={failCount} (PassLabel={passLabel != null}, FailLabel={failLabel != null})", "DEBUG");
        }

        // 🎯 새로운 헬퍼 메서드: 테스트 카운트 라벨 업데이트
        private void UpdateTestCountLabels(Panel devicePanel, bool isManualTest, int passCount, int failCount)
        {
            string passCountName = isManualTest ? "ManualPassCount" : "AutoPassCount";
            string failCountName = isManualTest ? "ManualFailCount" : "AutoFailCount";
            
            // Pass count 라벨 찾기 및 업데이트
            var passLabel = FindControlByName(devicePanel, passCountName) as Label;
            if (passLabel != null)
            {
                passLabel.Text = passCount.ToString();
                passLabel.ForeColor = Color.FromArgb(0x00, 0xFF, 0xA6); // 초록색
            }
            
            // Fail count 라벨 찾기 및 업데이트
            var failLabel = FindControlByName(devicePanel, failCountName) as Label;
            if (failLabel != null)
            {
                failLabel.Text = failCount.ToString();
                failLabel.ForeColor = Color.FromArgb(0xFF, 0x40, 0x40); // 빨간색
            }
        }

        // 🔍 Name으로 컨트롤 찾는 헬퍼 메서드
        private Control FindControlByName(Control parent, string name)
        {
            if (parent.Name == name) return parent;
            
            foreach (Control child in parent.Controls)
            {
                var found = FindControlByName(child, name);
                if (found != null) return found;
            }
            
            return null;
        }

        // 🎯 이벤트 핸들러들
        private void UpdateDeviceImeiData(UnifiedDeviceData device, string imei)
        {
            // 🎯 완전 조용한 데이터 업데이트 (로그도 생성하지 않음)
            device.IMEI_SN_Data.IMEI = imei;
            
            // 텍스트 입력 시에는 로그도 생성하지 않아서 완전히 조용함
            // sharedData.AddDeviceLog(device.Index, $"📝 IMEI 업데이트: {imei}", "DEBUG");
        }

        private void UpdateDeviceSnData(UnifiedDeviceData device, string serialNumber)
        {
            // 🎯 완전 조용한 데이터 업데이트 (로그도 생성하지 않음)
            device.IMEI_SN_Data.SerialNumber = serialNumber;
            
            // 텍스트 입력 시에는 로그도 생성하지 않아서 완전히 조용함
            // sharedData.AddDeviceLog(device.Index, $"📝 Serial Number 업데이트: {serialNumber}", "DEBUG");
        }

        private void WriteButton_Click(UnifiedDeviceData device)
        {
            // Write 작업 시뮬레이션
            device.IMEI_SN_Data.LastWriteTime = DateTime.Now;
            device.IMEI_SN_Data.IsWriteSuccess = true;
            
            sharedData.AddDeviceLog(device.Index, $"📝 IMEI/SN Write 완료: Device #{device.Index}", "SUCCESS");
            
            // 🎯 전체 UI 리프레시 대신 푸터 로그만 업데이트
            UpdateWriteStatusInFooter(device.Index, device);

            // 🎯 WriteButtonClicked 이벤트 발생
            WriteButtonClicked?.Invoke(device);
        }

        // 🔧 Save Result 버튼 클릭 이벤트
        private void SaveResultButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                dynamic tag = button.Tag;
                int deviceIndex = tag.DeviceIndex;

                var device = sharedData.GetDeviceByIndex(deviceIndex);
                if (device != null)
                {
                    sharedData.AddDeviceLog(device.Index, $"💾 MicInWrite Save Result 버튼 클릭됨: {device.Name} (Index: {deviceIndex})", "INFO");
                }
                else
                {
                    sharedData.AddDeviceLog(0, $"❌ MicInWrite Save: 디바이스를 찾을 수 없습니다 (Index: {deviceIndex})", "ERROR");
                }
            }
        }

        // 🎯 Write 완료 후 푸터 로그만 업데이트 (전체 UI 리프레시 없이)
        private void UpdateWriteStatusInFooter(int panelIndex, UnifiedDeviceData device)
        {
            if (panelIndex < 0 || panelIndex >= devicePanels.Count) return;

            var devicePanel = devicePanels[panelIndex];
            
            // 카드 푸터의 RichTextBox 찾기
            foreach (Control control in devicePanel.Controls)
            {
                if (control is Panel cardContainer)
                {
                    // 카드 푸터 찾기 (RichTextBox가 있는 패널로 찾기)
                    Panel? footer = null;
                    foreach (Control child in cardContainer.Controls)
                    {
                        if (child is Panel panel)
                        {
                            // 패널 안에 RichTextBox가 있으면 푸터임
                            foreach (Control panelChild in panel.Controls)
                            {
                                if (panelChild is RichTextBox)
                                {
                                    footer = panel;
                                    break;
                                }
                            }
                            if (footer != null) break;
                        }
                    }

                    if (footer != null)
                    {
                        // RichTextBox 찾기
                        foreach (Control footerChild in footer.Controls)
                        {
                            if (footerChild is RichTextBox logTextBox)
                            {
                                // 🎯 Write 완료 상태로 업데이트
                                string successMessage = $"✅ Write Finished: {device.IMEI_SN_Data.LastWriteTime:HH:mm:ss}";
                                
                                logTextBox.Clear();
                                logTextBox.SelectionColor = Color.FromArgb(0x00, 0xFF, 0xA6); // 초록색
                                logTextBox.AppendText(successMessage);
                                
                                break;
                            }
                        }
                    }
                    break;
                }
            }
        }

        // 📝 로그 패널에 라벨 추가하는 헬퍼 메서드
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

            // 🎯 스크롤바 숨기기 꼼수: 먼저 크기 늘리고 나서 포커스! (1페이지와 동일)
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

        // 🔄 기존 호환성 메서드들
        public void OnDeviceConnectionChanged(int index, SharedDeviceData device) { }
        public void OnDeviceDataChanged(int index, SharedDeviceData device) { }
        public void OnDeviceAdded(SharedDeviceData device) { }
        public void OnDeviceRemoved(int index) { }
        public void RefreshFromDeviceManager() { }
    }
} 
