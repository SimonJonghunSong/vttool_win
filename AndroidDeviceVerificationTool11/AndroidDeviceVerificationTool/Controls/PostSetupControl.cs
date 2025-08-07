using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using AndroidDeviceVerificationTool.Design;
using AndroidDeviceVerificationTool.Data;

namespace AndroidDeviceVerificationTool.Controls
{
    /// <summary>
    /// 📮 Post Setup 컨트롤 - PeripheralTestControl과 동일한 구조
    /// 4개 디바이스 고정 레이아웃, SharedApplicationData 중심 제어
    /// </summary>
    public partial class PostSetupControl : UserControl
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
        public event Action<UnifiedDeviceData>? SaveResultButtonClicked;

        public PostSetupControl()
        {
            // 🎨 **더블버퍼링 활성화** - 깜빡임 완전 제거
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.DoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.SupportsTransparentBackColor, false);
            
            // PeripheralTestControl과 동일하게 InitializeComponent() 제거
            sharedData = SharedApplicationData.Instance;
            
            CreateMainUI();
            SubscribeToSharedDataEvents();
            
            isInitialized = true;
            RefreshUI();
        }

        // 🎨 메인 UI 생성 (PeripheralTestControl과 동일한 구조)
        private void CreateMainUI()
        {
            this.BackColor = Color.FromArgb(47, 47, 55);
            this.Size = new Size(1600, 875);

            // 메인 패널 - PeripheralTestControl과 동일하게 Dock.Fill + 더블버퍼링
            mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(47, 47, 55),
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
            
            // PostSetupControl에서는 특별한 UI 업데이트가 필요 없음
            // 로그로만 확인
            sharedData?.AddDeviceLog(0, $"🏷️ PostSetup: 모델명 변경됨 - '{modelName}'", "INFO");
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
                // 🎯 PostSetup 데이터 변경 시 완전히 조용하게 처리
                // 버튼 클릭으로 인한 데이터 변경은 로그도 UI 업데이트도 없음
                
                // 완전히 조용한 처리 - 로그도 생성하지 않음
                // sharedData.AddDeviceLog(index, $"📮 PostSetup: Device #{index} 데이터 변경됨", "DEBUG");
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
            // RefreshUI(); // 제거
            
            // 🎯 완전히 조용한 처리 - 로그도 생성하지 않음
            // sharedData.AddDeviceLog(device.Index, $"📮 PostSetup: Device #{device.Index} 추가됨", "INFO");
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
            // sharedData.AddDeviceLog(index, $"📮 PostSetup: Device #{index} 제거됨", "INFO");
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

            // ✅ 디바이스 인덱스 매핑 수정 - 1,2탭과 동일하게 통일
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

        // 📝 Device Not Connected 푸터 - 푸터 크기 확장
        private Panel CreateNotConnectedFooter()
        {
            var footer = new Panel
            {
                Location = new Point(0, DEVICE_HEIGHT - 70), // 원래 위치 유지
                Size = new Size(DEVICE_WIDTH, 170), // 크기만 아래로 확장
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.FixedSingle
            };

            // 로그창 - Panel + Label 시스템으로 변경
            var logPanel = new Panel
            {
                Location = new Point(15, 15),
                Size = new Size(DEVICE_WIDTH - 30, 155),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None,
                AutoScroll = true,
                Name = "LogPanel_NotConnected"
            };

            // 🎯 더블버퍼링 적용
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, logPanel, new object[] { true });

            // 🎯 스크롤바 완전히 숨기고 가로 스크롤 완전 비활성화
            logPanel.AutoScroll = true;                         // 세로 스크롤만 활성화
            logPanel.VerticalScroll.Visible = false;            // 세로 스크롤바 숨기기
            logPanel.HorizontalScroll.Visible = false;          // 가로 스크롤바 숨기기
            logPanel.HorizontalScroll.Enabled = false;          // 가로 스크롤 완전 비활성화
            logPanel.AutoScrollMinSize = new Size(0, 0);        // 가로 스크롤 생성 방지

            // 초기 로그 메시지 추가
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
                // Post Setup 섹션 - 1페이지와 완전히 동일 (기존 크기 유지)
                var postSetupSection = CreatePostSetupSection(device, deviceIndex);
                postSetupSection.Location = new Point(20, 20); // 스크롤 패널 내부에서 여백 적용
                
                postSetupSection.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 210), 25);

                // 📝 연결된 디바이스용 카드 푸터 (Save 버튼 있음) - 1페이지와 완전히 동일
                var cardFooter = CreateCardFooter(device, deviceIndex);
                
                // 스크롤 영역 (Header~Footer 사이) - 1페이지와 완전히 동일
                int scrollY = cardHeader.Bottom;
                int scrollHeight = DEVICE_HEIGHT - cardHeader.Height - cardFooter.Height;
                var scrollContentPanel = new Panel
                {
                    Location = new Point(0, scrollY), // 스크롤 패널은 전체 너비 사용
                    Size = new Size(DEVICE_WIDTH, scrollHeight),
                    AutoScroll = true,
                    BackColor = Color.Transparent,
                };
                scrollContentPanel.Controls.Add(postSetupSection);

                // 1페이지와 완전히 동일한 스크롤바 숨기기 설정
                scrollContentPanel.AutoScroll = false;  

                scrollContentPanel.VerticalScroll.Maximum = 0;
                scrollContentPanel.VerticalScroll.Visible = false;

                scrollContentPanel.HorizontalScroll.Maximum = 0;
                scrollContentPanel.HorizontalScroll.Visible = false;

                scrollContentPanel.AutoScroll = true;

                cardFooter.Height -= 50;
                
                cardContainer.Controls.Add(scrollContentPanel);
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

            // Device ID 영역 - 매핑 정보 포함 (SharedApplicationData 사용)
            var mapping = sharedData.GetSlotMapping(deviceIndex);
            
            var deviceIdLabel = new Label
            {
                Text = !string.IsNullOrEmpty(device.DeviceId) ? 
                    $"{device.DeviceId}\n🎯{mapping.PubTopic}:{mapping.SubPort}" : 
                    $"Device ID\n🎯{mapping.PubTopic}:{mapping.SubPort}", // 기존 텍스트 + 매핑 정보 (2줄로)
                Location = new Point(215, 10), // 위치 살짝 위로 (15→10)
                Size = new Size(150, 35), // 높이만 늘림 (24→35)
                Font = new Font("Noto Sans KR", 9F, FontStyle.Regular), // 폰트 살짝 키움 (8F→9F)
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            header.Controls.Add(nameLabel);
            header.Controls.Add(deviceIdLabel);
            return header;
        }

        // 📮 Post Setup 섹션 생성 - 표준화된 위치와 크기
        private Panel CreatePostSetupSection(UnifiedDeviceData device, int deviceIndex)
        {
            var section = new Panel
            {
                // Location은 부모에서 설정됨 (Page 1과 동일하게)
                Size = new Size(DEVICE_WIDTH - 40, 210), // 표준화된 너비 (350 → 335)
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                BorderStyle = BorderStyle.None,
            };
            
            // 🎨 둥근 모서리 적용
            section.Region = CreateRoundedRegion(new Rectangle(0, 0, DEVICE_WIDTH - 40, 210), 25);

            // CSS 스펙의 IMEI, User ID, Pass 정보 표시
            CreatePostSetupInfo(section, device);

            // APN 설정 드롭다운과 Write 버튼
            CreateAPNControls(section, device);

            return section;
        }

        // 📮 Post Setup 정보 표시 - 통일된 레이아웃으로 정렬 개선
        private void CreatePostSetupInfo(Panel section, UnifiedDeviceData device)
        {
            const int ELEMENT_HEIGHT = 30;  // 표준 높이 (25 → 30)
            const int LABEL_WIDTH = 70;     // 라벨 너비 통일
            const int TEXTBOX_WIDTH = 200;  // 텍스트박스 너비 통일
            const int LEFT_MARGIN = 30;     // 좌측 여백 통일
            const int ELEMENT_SPACING = 40; // 줄간격 통일

            int yPos = 40;

            // APN 라벨 - 통일된 정렬
            var apnLabel = new Label
            {
                Text = "APN:",
                Location = new Point(LEFT_MARGIN, yPos + 2),
                Size = new Size(LABEL_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var apnValue = new TextBox
            {
                Text = !string.IsNullOrEmpty(device.ApnInfo.APN) ? device.ApnInfo.APN : "test1234",
                Location = new Point(LEFT_MARGIN + LABEL_WIDTH + 10, yPos),
                Size = new Size(TEXTBOX_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };

            apnValue.TextChanged += (sender, args) => { UpdateAPN(device, apnValue.Text); };

            section.Controls.Add(apnLabel);
            section.Controls.Add(apnValue);

            yPos += ELEMENT_SPACING;

            // User ID 라벨 - 통일된 정렬
            var userIdLabel = new Label
            {
                Text = "User ID:",
                Location = new Point(LEFT_MARGIN, yPos + 2),
                Size = new Size(LABEL_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var userIdValue = new TextBox
            {
                Text = !string.IsNullOrEmpty(device.ApnInfo.UserID) ? device.ApnInfo.UserID : "test1234",
                Location = new Point(LEFT_MARGIN + LABEL_WIDTH + 10, yPos),
                Size = new Size(TEXTBOX_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };

            userIdValue.TextChanged += (s, e) => UpdateDeviceUserId(device, userIdValue.Text);

            section.Controls.Add(userIdLabel);
            section.Controls.Add(userIdValue);

            yPos += ELEMENT_SPACING;

            // Pass 라벨 - 통일된 정렬
            var passLabel = new Label
            {
                Text = "Pass:",
                Location = new Point(LEFT_MARGIN, yPos + 2),
                Size = new Size(LABEL_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var passValue = new TextBox
            {
                Text = !string.IsNullOrEmpty(device.ApnInfo.Password) ? device.ApnInfo.Password : "testpass1231234",
                Location = new Point(LEFT_MARGIN + LABEL_WIDTH + 10, yPos),
                Size = new Size(TEXTBOX_WIDTH, ELEMENT_HEIGHT),
                Font = new Font("Noto Sans KR", 12F, FontStyle.Regular),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9), // 통일된 색상 (White → D9D9D9)
                BorderStyle = BorderStyle.FixedSingle,
                TabStop = false
            };

            passValue.TextChanged += (s, e) => UpdateDevicePassword(device, passValue.Text);

            section.Controls.Add(passLabel);
            section.Controls.Add(passValue);
        }
        
        private void UpdateAPN(UnifiedDeviceData device, string APN)
        {
            // 🎯 완전 조용한 데이터 업데이트 (로그도 생성하지 않음)
            device.ApnInfo.APN = APN;
            
            // 텍스트 입력 시에는 로그도 생성하지 않아서 완전히 조용함
            // sharedData.AddDeviceLog(device.Index, $"📝 IMEI 업데이트: {imei}", "DEBUG");
        }
        
        private void UpdateDeviceUserId(UnifiedDeviceData device, string userId)
        {
            // 🎯 완전 조용한 데이터 업데이트 (로그도 생성하지 않음)
            device.ApnInfo.UserID = userId;
            
            // 텍스트 입력 시에는 로그도 생성하지 않아서 완전히 조용함
            // sharedData.AddDeviceLog(device.Index, $"📝 IMEI 업데이트: {imei}", "DEBUG");
        }

        private void UpdateDevicePassword(UnifiedDeviceData device, string password)
        {
            // 🎯 완전 조용한 데이터 업데이트 (로그도 생성하지 않음)
            device.ApnInfo.Password = password;
            
            // 텍스트 입력 시에는 로그도 생성하지 않아서 완전히 조용함
            // sharedData.AddDeviceLog(device.Index, $"📝 IMEI 업데이트: {imei}", "DEBUG");
        }

        private void UpdateDeviceImeiData(UnifiedDeviceData device, string imei)
        {
            // 🎯 완전 조용한 데이터 업데이트 (로그도 생성하지 않음)
            device.IMEI_SN_Data.IMEI = imei;
            
            // 텍스트 입력 시에는 로그도 생성하지 않아서 완전히 조용함
            // sharedData.AddDeviceLog(device.Index, $"📝 IMEI 업데이트: {imei}", "DEBUG");
        }

        // 📮 Write 버튼 생성 - 통일된 레이아웃으로 정렬 개선
        private void CreateAPNControls(Panel section, UnifiedDeviceData device)
        {
            const int LEFT_MARGIN = 30;     // 좌측 여백 통일
            const int BUTTON_WIDTH = 280;   // 버튼 너비 통일
            const int BUTTON_HEIGHT = 35;   // 버튼 높이 통일

            // Write 버튼 - Tab 2와 동일한 스타일
            var writeButton = new Button
            {
                Location = new Point(LEFT_MARGIN, 160),
                Size = new Size(BUTTON_WIDTH, BUTTON_HEIGHT),
                Text = "Write",
                Font = new Font("Noto Sans KR", 12F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D),
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleCenter
            };
            writeButton.FlatAppearance.BorderSize = 0;
            writeButton.Click += (s, e) => APNWriteButton_Click(device);
            
            // 🎨 둥근 모서리 적용 - Tab 2와 동일
            writeButton.Region = CreateRoundedRegion(new Rectangle(0, 0, BUTTON_WIDTH, BUTTON_HEIGHT), 10);

            section.Controls.Add(writeButton);
        }

        // 📝 카드 푸터 생성 - 1페이지와 동일한 설정
        private Panel CreateCardFooter(UnifiedDeviceData device, int deviceIndex)
        {
            var footer = new Panel
            {
                Location = new Point(0, DEVICE_HEIGHT - 180), // 원래 위치 유지 
                Size = new Size(DEVICE_WIDTH, 280), // 크기만 아래로 확장
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

            // 로그창 - 푸터 전체 크기 사용 (1페이지처럼 크게 확장)
            var logPanel = new Panel
            {
                Location = new Point(15, 62),
                Size = new Size(DEVICE_WIDTH - 30, 160), // 대폭 확장하여 푸터 끝까지 사용
                BackColor = Color.FromArgb(0x57, 0x57, 0x65),
                BorderStyle = BorderStyle.None,
                Name = $"LogPanel_{deviceIndex}"
            };

            // 🎯 더블버퍼링 적용
            typeof(Panel).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, logPanel, new object[] { true });

            // 🎯 스크롤바 완전히 숨기고 가로 스크롤 완전 비활성화
            // 🎯 AutoTest scrollContentPanel과 완전히 동일한 스크롤바 설정! (1페이지와 동일)
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
                AddLogToPanel(logPanel, "APN write success ✅", Color.FromArgb(0x00, 0xFF, 0xA6));
            }

            footer.Controls.Add(actionButton);
            footer.Controls.Add(logPanel);
            return footer;
        }

        // 🎯 이벤트 핸들러들
        private void APNWriteButton_Click(UnifiedDeviceData device)
        {
            sharedData.AddDeviceLog(device.Index, $"📮 APN Write 완료: Device #{device.Index}", "SUCCESS");
            
            // 🎯 전체 UI 리프레시 대신 푸터 로그만 업데이트
            UpdatePostSetupStatusInFooter(device.Index, device); // device.Index - 1 제거

            // 🎯 WriteButtonClicked 이벤트 발생
            WriteButtonClicked?.Invoke(device);
        }

        // 🎯 PostSetup 완료 후 푸터 로그만 업데이트 (전체 UI 리프레시 없이)
        private void UpdatePostSetupStatusInFooter(int panelIndex, UnifiedDeviceData device)
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
                                // 🎯 manual_ap_apn 테스트 실행 완료 상태로 업데이트
                                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                                string successMessage = $"✅ manual_ap_apn 테스트 완료: {timestamp}";
                                
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

        // 🔧 Save Result 버튼 클릭 이벤트 (PeripheralTestControl과 동일)
        private void SaveResultButton_Click(object sender, EventArgs e)
        {
            if (sender is Button button && button.Tag != null)
            {
                dynamic tag = button.Tag;
                int deviceIndex = tag.DeviceIndex;

                var device = sharedData.GetDeviceByIndex(deviceIndex);
                if (device != null)
                {
                    sharedData.AddDeviceLog(device.Index, $"💾 PostSetup Save Result 버튼 클릭됨: {device.Name} (Index: {deviceIndex})", "INFO");
                    
                    // 🎯 SaveResultButtonClicked 이벤트 발생
                    SaveResultButtonClicked?.Invoke(device);
                }
                else
                {
                    sharedData.AddDeviceLog(0, $"❌ PostSetup Save: 디바이스를 찾을 수 없습니다 (Index: {deviceIndex})", "ERROR");
                }
            }
        }

        private void SaveResultDropdownButton_Click(UnifiedDeviceData device)
        {
            sharedData.AddDeviceLog(device.Index, $"📮 Save Result 드롭다운 클릭: Device #{device.Index}", "INFO");

            // 🎯 SaveResultButtonClicked 이벤트 발생
            SaveResultButtonClicked?.Invoke(device);
        }

        private void APNDropdownButton_Click(UnifiedDeviceData device)
        {
            sharedData.AddDeviceLog(device.Index, $"📮 APN 드롭다운 클릭: Device #{device.Index}", "INFO");
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
