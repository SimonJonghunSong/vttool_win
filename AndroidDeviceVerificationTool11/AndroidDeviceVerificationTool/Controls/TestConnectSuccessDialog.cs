using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AndroidDeviceVerificationTool.Data;

namespace AndroidDeviceVerificationTool.Controls
{
    /// <summary>
    /// 🎯 Test Connect Success Dialog - Figma 디자인 기반
    /// 1046x716 크기의 모달 다이얼로그
    /// </summary>
    public partial class TestConnectSuccessDialog : Form
    {
        // 🎨 디자인 상수들 (Figma 기반)
        private const int DIALOG_WIDTH = 1246;
        private const int DIALOG_HEIGHT = 716;
        
        // 컨트롤 참조
        private Panel mainContainer;
        private TabControl tabControl;
        private Panel contentPanel;
        
        // 🎯 다중 디바이스 지원
        private List<UnifiedDeviceData> connectedDevices;
        private UnifiedDeviceData currentDevice; // 현재 선택된 디바이스
        private Dictionary<UnifiedDeviceData, TestConnectData> deviceTestData = new Dictionary<UnifiedDeviceData, TestConnectData>();
        
        // 입력 필드들 (현재 선택된 디바이스용)
        private TextBox ipTextBox;
        private TextBox portTextBox;
        private TextBox idTextBox;
        private TextBox passTextBox;
        private TextBox wifiSsidTextBox;
        private TextBox bleNameTextBox;
        private TextBox apnTextBox;
        private TextBox userIdTextBox;
        private TextBox apnPassTextBox;
        private TextBox apkPathTextBox;
        
        // 테스트 항목 관리용
        private List<CheckBox> testCheckBoxes = new List<CheckBox>();
        private Panel autoTestItemsPanel;
        
        // 탭 관리용
        private TabControl deviceTabControl;
        private Panel tabContentPanel;
        
        // 🎯 단일 디바이스 생성자 (기존 호환성)
        public TestConnectSuccessDialog(UnifiedDeviceData device) 
            : this(new List<UnifiedDeviceData> { device })
        {   
        }
        
        // 🎯 다중 디바이스 생성자 (새로운 기능)
        public TestConnectSuccessDialog(List<UnifiedDeviceData> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                throw new ArgumentException("디바이스 목록이 비어있습니다.");
            }
            
            connectedDevices = devices.Where(d => d.IsConnected).ToList();
            if (connectedDevices.Count == 0)
            {
                throw new ArgumentException("연결된 디바이스가 없습니다.");
            }
            
            // 각 디바이스의 TestConnectData 초기화
            foreach (var device in connectedDevices)
            {
                var testData = device.TestConnectData ?? new TestConnectData(device.Name, device.DeviceId);
                if (device.TestConnectData == null)
                {
                    device.TestConnectData = testData;
                }
                deviceTestData[device] = testData;
            }
            
            // 첫 번째 디바이스를 현재 선택으로 설정
            currentDevice = connectedDevices[0];
            
            InitializeDialog();
            LoadDataToControls();
        }
        
        // 🔧 현재 디바이스의 TestConnectData 가져오기
        private TestConnectData GetCurrentTestData()
        {
            return deviceTestData.ContainsKey(currentDevice) ? deviceTestData[currentDevice] : currentDevice.TestConnectData;
        }
        
        // 🔧 현재 디바이스 변경
        private void SetCurrentDevice(UnifiedDeviceData device)
        {
            if (connectedDevices.Contains(device))
            {
                // 이전 디바이스의 데이터 저장
                SaveDataFromControls();
                
                // 현재 디바이스 변경
                currentDevice = device;
                
                // 탭 UI 새로고침 (활성 탭 표시 업데이트)
                RefreshTabSection();
                
                // 새 디바이스 데이터 로드
                LoadDataToControls();
                
                // 테스트 항목 새로고침
                RefreshTestItems();
            }
        }
        
        // 🔄 탭 섹션 새로고침
        private void RefreshTabSection()
        {
            // 기존 탭 섹션 제거
            var tabSectionToRemove = mainContainer.Controls
                .OfType<Panel>()
                .FirstOrDefault(p => p.Location.Y == 52 && p.Size.Height == 48);
            
            if (tabSectionToRemove != null)
            {
                mainContainer.Controls.Remove(tabSectionToRemove);
                tabSectionToRemove.Dispose();
            }
            
            // 새 탭 섹션 생성
            CreateTabSection();
        }
        
        private void InitializeDialog()
        {
            // 🎨 기본 폼 설정
            this.Text = "Test Connect Success";
            this.Size = new Size(DIALOG_WIDTH, DIALOG_HEIGHT);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(0x2F, 0x2F, 0x37); // #2F2F37
            this.ShowInTaskbar = false;
            this.TopMost = true;
            
            // 🌟 그림자 효과를 위한 드롭 섀도우 (간단한 테두리로 대체)
            this.Paint += (s, e) =>
            {
                using (var pen = new Pen(Color.FromArgb(50, 0, 0, 0), 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, this.Width - 1, this.Height - 1);
                }
            };
            
            CreateMainContainer();
            CreateHeaderSection();
            CreateTabSection();
            CreateContentSection();
            CreateButtonSection();
        }
        
        // 🎨 메인 컨테이너 생성
        private void CreateMainContainer()
        {
            mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(0x2F, 0x2F, 0x37),
                Padding = new Padding(16)
            };
            this.Controls.Add(mainContainer);
        }
        
        // 📱 헤더 섹션 생성
        private void CreateHeaderSection()
        {
            var headerPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(DIALOG_WIDTH - 32, 52),
                BackColor = Color.Transparent
            };
            
            // ✏️ Header 라벨
            var headerLabel = new Label
            {
                Text = "⚙️ Test Connect Settings",
                Location = new Point(16, 12),
                Size = new Size(250, 35),
                Font = new Font("Noto Sans KR", 20F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };
            
            // Connected Devices 카운트 라벨 (실제 연결된 것만 카운트)
            int actualConnectedCount = connectedDevices.Count(d => d.IsConnected);
            var deviceCountLabel = new Label
            {
                Text = $"Connected Devices: {actualConnectedCount}",
                Location = new Point(DIALOG_WIDTH - 700, 16),
                Size = new Size(500, 40),
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                TextAlign = ContentAlignment.MiddleRight,
                BackColor = Color.Transparent
            };
            
            headerPanel.Controls.Add(headerLabel);
            headerPanel.Controls.Add(deviceCountLabel);
            mainContainer.Controls.Add(headerPanel);
        }
        
        // 📑 디바이스별 탭 섹션 생성
        private void CreateTabSection()
        {
            var tabPanel = new Panel
            {
                Location = new Point(0, 52),
                Size = new Size(DIALOG_WIDTH - 32, 48),
                BackColor = Color.FromArgb(0x2F, 0x2F, 0x37)
            };
            
            // 📱 연결된 디바이스별 탭 생성
            int tabWidth = Math.Min(200, (DIALOG_WIDTH - 64) / connectedDevices.Count); // 최대 200px, 최소한 모든 탭이 들어가도록
            
            for (int i = 0; i < connectedDevices.Count; i++)
            {
                var device = connectedDevices[i];
                bool isActive = device == currentDevice;
                
                var deviceTab = new Panel
                {
                    Location = new Point(i * tabWidth, 0),
                    Size = new Size(tabWidth, 48),
                    BackColor = Color.FromArgb(0x2F, 0x2F, 0x37),
                    Tag = device,
                    Cursor = Cursors.Hand
                };
                
                var deviceLabel = new Label
                {
                    Text = device.Name,
                    Location = new Point(10, 14),
                    Size = new Size(tabWidth - 20, 20),
                    Font = new Font("Noto Sans KR", 12F, FontStyle.Regular),
                    ForeColor = isActive ? Color.White : Color.FromArgb(0xB0, 0xB0, 0xB0),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                
                // 🔗 연결 상태 표시
                var statusDot = new Label
                {
                    Text = "●",
                    Location = new Point(tabWidth - 25, 12),
                    Size = new Size(15, 15),
                    Font = new Font("Arial", 12F, FontStyle.Bold),
                    ForeColor = device.IsConnected ? Color.FromArgb(0x00, 0xFF, 0xA6) : Color.FromArgb(0xFF, 0x40, 0x40),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };
                
                // 활성 탭 인디케이터
                if (isActive)
                {
                    var indicator = new Panel
                    {
                        Location = new Point(0, 46),
                        Size = new Size(tabWidth, 2),
                        BackColor = Color.FromArgb(0xF3, 0x71, 0x21) // #F37121
                    };
                    deviceTab.Controls.Add(indicator);
                }
                
                // 탭 클릭 이벤트 (캡처된 디바이스로 안전하게 전달)
                var capturedDevice = device; // 클로저를 위한 지역 변수
                deviceTab.Click += (s, e) => 
                {
                    if (capturedDevice != currentDevice)
                    {
                        SetCurrentDevice(capturedDevice);
                    }
                };
                deviceLabel.Click += (s, e) => 
                {
                    if (capturedDevice != currentDevice)
                    {
                        SetCurrentDevice(capturedDevice);
                    }
                };
                statusDot.Click += (s, e) => 
                {
                    if (capturedDevice != currentDevice)
                    {
                        SetCurrentDevice(capturedDevice);
                    }
                };
                
                deviceTab.Controls.Add(deviceLabel);
                deviceTab.Controls.Add(statusDot);
                tabPanel.Controls.Add(deviceTab);
            }
            
            // 구분선
            var divider = new Panel
            {
                Location = new Point(0, 47),
                Size = new Size(DIALOG_WIDTH - 32, 1),
                BackColor = Color.FromArgb(0x57, 0x57, 0x65)
            };
            
            tabPanel.Controls.Add(divider);
            mainContainer.Controls.Add(tabPanel);
        }
        
        // 📊 컨텐츠 섹션 생성
        private void CreateContentSection()
        {
            contentPanel = new Panel
            {
                Location = new Point(0, 100),
                Size = new Size(DIALOG_WIDTH - 32, 540),
                BackColor = Color.Transparent,
                AutoScroll = false
            };
            
            CreateAutoTestSection();
            CreateDBAccessSection();
            CreateAPKSection();
            CreateWifiSection();
            CreateAPNSection();
            
            mainContainer.Controls.Add(contentPanel);
        }
        
        // 🤖 Auto Test 섹션 생성
        private void CreateAutoTestSection()
        {
            autoTestItemsPanel = new Panel
            {
                Location = new Point(30, 0),
                Size = new Size(400, 508),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52), // #464652
                BorderStyle = BorderStyle.None
            };
            
            // 헤더
            var header = new Panel
            {
                Location = new Point(30, 0),
                Size = new Size(400, 48),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52)
            };
            
            var headerTitle = new Label
            {
                Text = "Auto-test item",
                Location = new Point(16, 16),
                Size = new Size(126, 40),
                Font = new Font("Noto Sans KR", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent
            };
            
            header.Controls.Add(headerTitle);
            autoTestItemsPanel.Controls.Add(header);
            
            // 테스트 항목들 (2열 레이아웃)
            CreateTestItems(autoTestItemsPanel);
            
            contentPanel.Controls.Add(autoTestItemsPanel);
        }
        
        // 🧪 테스트 항목들 생성 (TestConnectData 기반)
        private void CreateTestItems(Panel parent)
        {
            testCheckBoxes.Clear();
            var currentTestData = GetCurrentTestData();
            
            for (int i = 0; i < currentTestData.TestItems.Count; i++)
            {
                var testItem = currentTestData.TestItems[i];
                int col = testItem.Column;
                int row = testItem.Row;
                
                var testPanel = new Panel
                {
                    Location = new Point(16 + col * 163, 68 + row * 36),
                    Size = new Size(139, 28),
                    BackColor = Color.Transparent
                };
                
                var testLabel = new Label
                {
                    Text = $"{row + 1}. {testItem.Name}:",
                    Location = new Point(0, 0),
                    Size = new Size(102, 28),
                    Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                    ForeColor = testItem.IsEnabled ? Color.FromArgb(0xD9, 0xD9, 0xD9) : Color.FromArgb(97, 0xD9, 0xD9, 0xD9), // 38% opacity
                    BackColor = Color.Transparent
                };
                
                var checkBox = new CheckBox
                {
                    Location = new Point(115, 0),
                    Size = new Size(24, 24),
                    Checked = testItem.IsSelected,
                    Enabled = testItem.IsEnabled,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Appearance = Appearance.Button,
                    Tag = i // 인덱스 저장
                };
                
                testCheckBoxes.Add(checkBox);
                
                // 체크박스 커스텀 그리기
                checkBox.Paint += (s, e) =>
                {
                    var cb = s as CheckBox;
                    var rect = new Rectangle(3, 3, 18, 18);
                    
                    // 배경
                    if (cb.Checked && cb.Enabled)
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(0xF3, 0x71, 0x21)), rect);
                    }
                    else
                    {
                        e.Graphics.FillRectangle(new SolidBrush(Color.White), rect);
                        if (!cb.Enabled)
                        {
                            e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(97, 255, 255, 255)), rect);
                        }
                    }
                    
                    // 테두리
                    e.Graphics.DrawRectangle(new Pen(Color.FromArgb(0x79, 0x74, 0x7E)), rect);
                    
                    // 체크 표시
                    if (cb.Checked && cb.Enabled)
                    {
                        using (var pen = new Pen(Color.White, 2))
                        {
                            e.Graphics.DrawLines(pen, new Point[] {
                                new Point(rect.X + 4, rect.Y + 9),
                                new Point(rect.X + 8, rect.Y + 13),
                                new Point(rect.X + 14, rect.Y + 5)
                            });
                        }
                    }
                };
                
                // 체크박스 이벤트 - TestConnectData 업데이트
                int capturedIndex = i; // 클로저를 위한 지역 변수
                checkBox.CheckedChanged += (s, e) =>
                {
                    var cb = s as CheckBox;
                    if (cb.Tag is int index)
                    {
                        GetCurrentTestData().SetTestItemSelected(index, cb.Checked);
                        cb.Invalidate(); // 다시 그리기
                    }
                };
                
                testPanel.Controls.Add(testLabel);
                testPanel.Controls.Add(checkBox);
                parent.Controls.Add(testPanel);
            }
            
            // 테스트 항목 추가/제거 버튼들 생성
            CreateTestManagementButtons(parent);
        }
        
        // 🔧 테스트 관리 버튼들 생성
        private void CreateTestManagementButtons(Panel parent)
        {
            var buttonPanel = new Panel
            {
                Location = new Point(16, 68 + 8 * 36 + 10),
                Size = new Size(300, 30),
                BackColor = Color.Transparent
            };
            
            var selectAllButton = new Button
            {
                Location = new Point(140, 0),
                Size = new Size(70, 25),
                Text = "Select All",
                Font = new Font("Noto Sans KR", 9F, FontStyle.Regular),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            selectAllButton.FlatAppearance.BorderSize = 0;
            selectAllButton.Click += (s, e) => 
            {
                GetCurrentTestData().SetAllTestItemsSelected(true);
                RefreshTestItems();
            };
            
            var clearAllButton = new Button
            {
                Location = new Point(220, 0),
                Size = new Size(70, 25),
                Text = "Clear All",
                Font = new Font("Noto Sans KR", 9F, FontStyle.Regular),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            clearAllButton.FlatAppearance.BorderSize = 0;
            clearAllButton.Click += (s, e) => 
            {
                GetCurrentTestData().SetAllTestItemsSelected(false);
                RefreshTestItems();
            };
            
            buttonPanel.Controls.Add(selectAllButton);
            buttonPanel.Controls.Add(clearAllButton);
            parent.Controls.Add(buttonPanel);
        }
        
        // 🗄️ DB Access 섹션 생성
        private void CreateDBAccessSection()
        {
            var dbPanel = new Panel
            {
                Location = new Point(454, 0),
                Size = new Size(800, 212),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52)
            };
            
            // 헤더
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(800, 48),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52)
            };
            
            var headerTitle = new Label
            {
                Text = "DB access info",
                Location = new Point(16, 16),
                Size = new Size(125, 45),
                Font = new Font("Noto Sans KR", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent
            };
            
            header.Controls.Add(headerTitle);
            dbPanel.Controls.Add(header);
            
            // IP 필드
            CreateLabeledTextBox(dbPanel, "IP", 14, 56, out ipTextBox);
            CreateLabeledTextBox(dbPanel, "Port", 340, 56, out portTextBox);
            
            // ID 필드
            CreateLabeledTextBox(dbPanel, "ID", 14, 108, out idTextBox);
            CreateLabeledTextBox(dbPanel, "Pass", 340, 108, out passTextBox);
            
            // Connect 버튼
            var connectBtn = new Button
            {
                Location = new Point(526, 160),
                Size = new Size(120, 40),
                Text = "DB Connect",
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D),
                FlatStyle = FlatStyle.Flat
            };
            connectBtn.FlatAppearance.BorderSize = 0;
            dbPanel.Controls.Add(connectBtn);
            
            contentPanel.Controls.Add(dbPanel);
        }
        
        // 📱 APK 섹션 생성
        private void CreateAPKSection()
        {
            var apkPanel = new Panel
            {
                Location = new Point(454, 232),
                Size = new Size(800, 108),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52)
            };
            
            // 헤더
            var header = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(660, 48),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52)
            };
            
            var headerTitle = new Label
            {
                Text = "Target Device Test APK",
                Location = new Point(16, 8),
                Size = new Size(400, 35),
                Font = new Font("Noto Sans KR", 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent
            };
            
            header.Controls.Add(headerTitle);
            apkPanel.Controls.Add(header);
            
            // Browse 버튼
            var browseBtn = new Button
            {
                Location = new Point(16, 56),
                Size = new Size(120, 40),
                Text = "Browse",
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D),
                FlatStyle = FlatStyle.Flat
            };
            browseBtn.FlatAppearance.BorderSize = 0;
            browseBtn.Click += BrowseAPK_Click;
            
            // APK 경로 텍스트박스
            apkPathTextBox = new TextBox
            {
                Location = new Point(148, 56),
                Size = new Size(498, 40),
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Enabled = false
            };
            
            apkPanel.Controls.Add(browseBtn);
            apkPanel.Controls.Add(apkPathTextBox);
            contentPanel.Controls.Add(apkPanel);
        }
        
        // 📶 Wi-Fi/BLE 섹션 생성
        private void CreateWifiSection()
        {
            var wifiPanel = new Panel
            {
                Location = new Point(454, 360),
                Size = new Size(800, 64),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52)
            };
            
            CreateLabeledTextBox(wifiPanel, "Wi-Fi SSID", 16, 12, out wifiSsidTextBox);
            CreateLabeledTextBox(wifiPanel, "BLE Name", 342, 12, out bleNameTextBox);
            
            contentPanel.Controls.Add(wifiPanel);
        }
        
        // 🌐 APN 섹션 생성
        private void CreateAPNSection()
        {
            var apnPanel = new Panel
            {
                Location = new Point(454, 444),
                Size = new Size(800, 64),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52)
            };
            
            CreateLabeledTextBox(apnPanel, "APN", 2, 12, out apnTextBox);
            CreateLabeledTextBox(apnPanel, "User ID", 250, 12, out userIdTextBox);
            CreateLabeledTextBox(apnPanel, "Pass", 500, 12, out apnPassTextBox);
            
            contentPanel.Controls.Add(apnPanel);
        }
        
        // 🏷️ 라벨과 텍스트박스 생성 헬퍼
        private void CreateLabeledTextBox(Panel parent, string labelText, int x, int y, out TextBox textBox)
        {
            var label = new Label
            {
                Text = labelText,
                Location = new Point(x, y),
                Size = new Size(60, 50),
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent
            };
            
            textBox = new TextBox
            {
                Location = new Point(x + 70, y - 2),
                Size = new Size(170, 50),
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            
            parent.Controls.Add(label);
            parent.Controls.Add(textBox);
        }
        
        // 🔘 버튼 섹션 생성
        private void CreateButtonSection()
        {
            var buttonPanel = new Panel
            {
                Location = new Point(0, 616),
                Size = new Size(DIALOG_WIDTH - 32, 64),
                BackColor = Color.Transparent
            };
            
            // Version 라벨
            var versionLabel = new Label
            {
                Text = "Version: 1.0",
                Location = new Point(468, 22),
                Size = new Size(128, 40),
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                ForeColor = Color.FromArgb(77, 0xD9, 0xD9, 0xD9), // 30% opacity
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };
            
            // Cancel 버튼
            var cancelBtn = new Button
            {
                Location = new Point(DIALOG_WIDTH - 280, 24),
                Size = new Size(120, 40),
                Text = "Cancel",
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                BackColor = Color.Transparent,
                ForeColor = Color.FromArgb(0xF3, 0x71, 0x21),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            cancelBtn.FlatAppearance.BorderColor = Color.FromArgb(0xF3, 0x71, 0x21);
            cancelBtn.FlatAppearance.BorderSize = 1;
            
            // OK 버튼
            var okBtn = new Button
            {
                Location = new Point(DIALOG_WIDTH - 152, 24),
                Size = new Size(120, 40),
                Text = "OK",
                Font = new Font("Noto Sans KR", 14F, FontStyle.Regular),
                BackColor = Color.FromArgb(0xF3, 0x71, 0x21),
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D),
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.OK
            };
            okBtn.FlatAppearance.BorderSize = 0;
            okBtn.Click += OkButton_Click;
            
            buttonPanel.Controls.Add(versionLabel);
            buttonPanel.Controls.Add(cancelBtn);
            buttonPanel.Controls.Add(okBtn);
            mainContainer.Controls.Add(buttonPanel);
        }
        
        // 📝 데이터를 컨트롤에 로드
        private void LoadDataToControls()
        {
            var currentTestData = GetCurrentTestData();
            
            // 입력 필드들에 기본값 설정
            if (currentTestData.DatabaseInfo != null)
            {
                if (ipTextBox != null) ipTextBox.Text = currentTestData.DatabaseInfo.IP;
                if (portTextBox != null) portTextBox.Text = currentTestData.DatabaseInfo.Port;
                if (idTextBox != null) idTextBox.Text = currentTestData.DatabaseInfo.ID;
                if (passTextBox != null) passTextBox.Text = currentTestData.DatabaseInfo.Password;
            }
            
            if (currentTestData.ApkInfo != null && apkPathTextBox != null)
            {
                apkPathTextBox.Text = currentTestData.ApkInfo.FilePath;
            }
            
            if (currentTestData.WirelessInfo != null)
            {
                if (wifiSsidTextBox != null) wifiSsidTextBox.Text = currentTestData.WirelessInfo.WifiSSID;
                if (bleNameTextBox != null) bleNameTextBox.Text = currentTestData.WirelessInfo.BleName;
            }
            
            if (currentDevice.ApnInfo != null)
            {
                if (apnTextBox != null) apnTextBox.Text = currentDevice.ApnInfo.APN;
                if (userIdTextBox != null) userIdTextBox.Text = currentDevice.ApnInfo.UserID;
                if (apnPassTextBox != null) apnPassTextBox.Text = currentDevice.ApnInfo.Password;
            }
        }
        
        // 🔄 컨트롤에서 데이터로 저장
        private void SaveDataFromControls()
        {
            var currentTestData = GetCurrentTestData();
            
            // DB 정보 저장
            if (currentTestData.DatabaseInfo != null)
            {
                currentTestData.DatabaseInfo.IP = ipTextBox?.Text ?? "";
                currentTestData.DatabaseInfo.Port = portTextBox?.Text ?? "";
                currentTestData.DatabaseInfo.ID = idTextBox?.Text ?? "";
                currentTestData.DatabaseInfo.Password = passTextBox?.Text ?? "";
            }
            
            // APK 정보 저장
            if (currentTestData.ApkInfo != null)
            {
                currentTestData.ApkInfo.FilePath = apkPathTextBox?.Text ?? "";
            }
            
            // 무선 정보 저장
            if (currentTestData.WirelessInfo != null)
            {
                currentTestData.WirelessInfo.WifiSSID = wifiSsidTextBox?.Text ?? "";
                currentTestData.WirelessInfo.BleName = bleNameTextBox?.Text ?? "";
            }
            
            // APN 정보 저장
            if (currentDevice.ApnInfo != null)
            {
                currentDevice.ApnInfo.APN = apnTextBox?.Text ?? "";
                currentDevice.ApnInfo.UserID = userIdTextBox?.Text ?? "";
                currentDevice.ApnInfo.Password = apnPassTextBox?.Text ?? "";
            }
        }
        
        // 🧪 테스트 항목 추가 버튼 클릭
        private void AddTestButton_Click(object sender, EventArgs e)
        {
            var currentTestData = GetCurrentTestData();
            string testName = ShowInputDialog("새 테스트 항목의 이름을 입력하세요:", 
                                             "테스트 항목 추가", 
                                             $"Test {currentTestData.TestItems.Count + 1}");
                
            if (!string.IsNullOrWhiteSpace(testName))
            {
                currentTestData.AddTestItem(testName, false, true);
                RefreshTestItems();
            }
        }
        
        // 📱 APK 파일 브라우저 클릭
        private void BrowseAPK_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "APK files (*.apk)|*.apk|All files (*.*)|*.*";
                openFileDialog.Title = "Select APK File";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                
                if (openFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    apkPathTextBox.Text = openFileDialog.FileName;
                    GetCurrentTestData().ApkInfo.FilePath = openFileDialog.FileName;
                }
            }
        }
        
        // ✅ OK 버튼 클릭 이벤트 (모든 디바이스 저장)
        private void OkButton_Click(object sender, EventArgs e)
        {
            // 현재 선택된 디바이스의 UI 상태를 저장
            SaveDataFromControls();
            
            // 🎯 모든 연결된 디바이스의 데이터 검증 및 저장
            var allValid = true;
            var errorMessages = new List<string>();
            
            foreach (var device in connectedDevices)
            {
                var testData = deviceTestData[device];
                var validationResult = testData.Validate();
                
                if (!validationResult.IsValid)
                {
                    allValid = false;
                    errorMessages.Add($"[{device.Name}] {validationResult.GetSummary()}");
                }
                else
                {
                    // 유효한 데이터는 디바이스에 저장
                    device.TestConnectData = testData;
                    
                    // DeviceId 업데이트 (변경된 경우)
                    if (!string.IsNullOrEmpty(testData.SelectedDeviceId))
                    {
                        device.DeviceId = testData.SelectedDeviceId;
                    }
                    
                    // 수정 시간 업데이트
                    testData.ModifiedTime = DateTime.Now;
                }
            }
            
            // 검증 실패한 디바이스가 있으면 에러 메시지 표시
            if (!allValid)
            {
                string fullErrorMessage = $"다음 디바이스들에 입력 오류가 있습니다:\n\n{string.Join("\n\n", errorMessages)}";
                MessageBox.Show(this, fullErrorMessage, "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return; // 다이얼로그를 닫지 않음
            }
            
            // 모든 검증 통과 - 다이얼로그 닫기
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        // 📝 간단한 입력 다이얼로그
        private string ShowInputDialog(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 400,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };
            
            Label textLabel = new Label() { Left = 20, Top = 20, Width = 350, Text = text };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 300, Text = defaultValue };
            Button confirmation = new Button() { Text = "OK", Left = 250, Width = 70, Top = 80, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 170, Width = 70, Top = 80, DialogResult = DialogResult.Cancel };
            
            confirmation.Click += (sender, e) => { prompt.Close(); };
            cancel.Click += (sender, e) => { prompt.Close(); };
            
            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(cancel);
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;
            
            return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text : "";
        }
        
        // 🗑️ 테스트 항목 제거 버튼 클릭
        private void RemoveTestButton_Click(object sender, EventArgs e)
        {
            var selectedItems = new List<int>();
            for (int i = 0; i < testCheckBoxes.Count; i++)
            {
                if (testCheckBoxes[i].Checked)
                {
                    selectedItems.Add(i);
                }
            }
            
            if (selectedItems.Count > 0)
            {
                var result = MessageBox.Show(this, $"선택된 {selectedItems.Count}개의 테스트 항목을 삭제하시겠습니까?", 
                                           "테스트 항목 삭제", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    var currentTestData = GetCurrentTestData();
                    // 역순으로 삭제 (인덱스 변경 방지)
                    for (int i = selectedItems.Count - 1; i >= 0; i--)
                    {
                        currentTestData.RemoveTestItem(selectedItems[i]);
                    }
                    RefreshTestItems();
                }
            }
            else
            {
                MessageBox.Show(this, "삭제할 테스트 항목을 선택해주세요.", "알림", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        
        // 🔄 테스트 항목들 새로고침
        private void RefreshTestItems()
        {
            if (autoTestItemsPanel != null)
            {
                // 기존 테스트 항목들 제거
                var itemsToRemove = new List<Control>();
                foreach (Control control in autoTestItemsPanel.Controls)
                {
                    if (control.Location.Y >= 68) // 헤더 아래 항목들만
                    {
                        itemsToRemove.Add(control);
                    }
                }
                
                foreach (var item in itemsToRemove)
                {
                    autoTestItemsPanel.Controls.Remove(item);
                    item.Dispose();
                }
                
                // 새로 생성
                CreateTestItems(autoTestItemsPanel);
            }
        }
        
        // 🎯 현재 디바이스의 통합 데이터 구조체 가져오기
        public TestConnectData GetTestConnectData()
        {
            SaveDataFromControls();
            return GetCurrentTestData();
        }
        
        // 🎯 모든 디바이스의 TestConnectData 가져오기 (새로운 기능)
        public Dictionary<UnifiedDeviceData, TestConnectData> GetAllTestConnectData()
        {
            SaveDataFromControls();
            return new Dictionary<UnifiedDeviceData, TestConnectData>(deviceTestData);
        }
        
        // 🔄 기존 호환성을 위한 메서드들 (현재 선택된 디바이스 기준)
        public Dictionary<string, bool> GetTestSelections()
        {
            SaveDataFromControls();
            return GetCurrentTestData().ToTestSelectionDictionary();
        }
        
        // 📊 DB 연결 정보 가져오기 (기존 호환성)
        public (string ip, string port, string id, string pass) GetDBInfo()
        {
            SaveDataFromControls();
            var currentTestData = GetCurrentTestData();
            return (currentTestData.DatabaseInfo.IP, 
                   currentTestData.DatabaseInfo.Port, 
                   currentTestData.DatabaseInfo.ID, 
                   currentTestData.DatabaseInfo.Password);
        }
        
        // 📱 APK 경로 가져오기 (기존 호환성)
        public string GetAPKPath()
        {
            SaveDataFromControls();
            return GetCurrentTestData().ApkInfo.FilePath;
        }
        
        // 📶 Wi-Fi/BLE 정보 가져오기 (기존 호환성)
        public (string wifiSsid, string bleName) GetWifiInfo()
        {
            SaveDataFromControls();
            var currentTestData = GetCurrentTestData();
            return (currentTestData.WirelessInfo.WifiSSID, 
                   currentTestData.WirelessInfo.BleName);
        }
        
        // 🌐 APN 정보 가져오기 (기존 호환성)
        public (string apn, string userId, string pass) GetAPNInfo()
        {
            SaveDataFromControls();
            return (currentDevice.ApnInfo.APN, 
                currentDevice.ApnInfo.UserID, 
                currentDevice.ApnInfo.Password);
        }
    }
} 