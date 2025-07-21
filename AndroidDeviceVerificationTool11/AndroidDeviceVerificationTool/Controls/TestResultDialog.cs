using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AndroidDeviceVerificationTool.Controls
{
    /// <summary>
    /// 🎯 테스트 결과 입력/선택 팝업 다이얼로그
    /// CSS 디자인 기반 Material Design 스타일
    /// </summary>
    public partial class TestResultDialog : Form
    {
        private Panel mainContainer;
        private Panel dialogContent;
        private Panel resultSection;
        private Label resultLabel;
        private Label testNameLabel;
        private Label descriptionLabel;
        private Panel radioButtonsPanel;
        private CustomRadioButton passRadioButton;
        private CustomRadioButton failRadioButton;
        private Panel buttonsPanel;
        private Button cancelButton;
        private Button saveButton;
        
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool TestResult { get; private set; } = false;
        
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool DialogConfirmed { get; private set; } = false;
        
        public TestResultDialog(string testName, string description = "", string testResult = "", string res_param="")
        {
            InitializeComponent(testName, description, testResult, res_param);
        }
        
        private void InitializeComponent(string testName, string description, string testResult, string res_param)
        {
            // 🎨 Form 기본 설정
            this.Text = "Test Result";
            this.Size = new Size(520, 450); // Result 섹션 확장에 맞춰 크기 증가
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(0x2F, 0x2F, 0x37); // #2F2F37
            this.ShowInTaskbar = false;
            this.TopMost = true;
            
            // 🎯 Drop Shadow 효과를 위한 둥근 모서리
            this.Region = CreateRoundedRegion(this.ClientRectangle, 8);

            
            string testResult2 = testResult + '\n' + res_param;
            if (res_param.Equals("")) testResult2 = "No MQTT Msg Received";


            CreateMainContainer();
            CreateDialogContent(testName, description);
            CreateResultSection(testResult2);
            CreateRadioButtons();
            CreateButtons();
            
            // 초기 포커스 설정
            this.ActiveControl = passRadioButton;
        }
        
        // 🎨 둥근 모서리 Region 생성
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
        
        // 🎯 메인 컨테이너 생성 (CSS의 메인 다이얼로그)
        private void CreateMainContainer()
        {
            mainContainer = new Panel
            {
                Location = new Point(20, 10),
                Size = new Size(480, 450), // 폼 크기에 맞춰 조정
                BackColor = Color.FromArgb(0x2F, 0x2F, 0x37), // #2F2F37
                Padding = new Padding(10),
                BorderStyle = BorderStyle.None
            };
            
            this.Controls.Add(mainContainer);
        }
        
        // 📝 Dialog Content 영역 생성
        private void CreateDialogContent(string testName, string description)
        {
            dialogContent = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(460, 480), // 메인 컨테이너에 맞춰 조정
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            
            // 🎯 테스트 이름과 설명을 한 줄에 중앙정렬로 표현
            string combinedText = string.IsNullOrEmpty(description) 
                ? testName 
                : $"{testName} - {description}";
                
            testNameLabel = new Label
            {
                Text = combinedText,
                Location = new Point(0, 10),
                Size = new Size(460, 30), // 전체 폭 사용, 고정 높이
                Font = new Font(new FontFamily("Noto Sans KR"), 13F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter, // ✅ 중앙정렬
                AutoSize = false // 고정 크기로 중앙정렬 유지
            };
            
            dialogContent.Controls.Add(testNameLabel);
            mainContainer.Controls.Add(dialogContent);
            
            // 안내문구 추가 - 중앙정렬
            var selectGuideLabel = new Label
            {
                Text = "결과를 선택하세요",
                Location = new Point(0, 220), // Result 섹션 아래로 이동 (180 → 220)
                Size = new Size(460, 25), // 고정 크기로 중앙정렬
                Font = new Font(new FontFamily("Noto Sans KR"), 13F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter, // ✅ 중앙정렬
                AutoSize = false // 고정 크기로 중앙정렬 유지
            };
            dialogContent.Controls.Add(selectGuideLabel);
        }
        
        // 📊 Result 섹션 생성 (CSS의 Group 1664) - 테스트 결과 표시
        private void CreateResultSection(string testResult)
        {
            resultSection = new Panel
            {
                Location = new Point(0, 50), // testNameLabel 바로 아래 위치
                Size = new Size(460, 160), // 높이를 더 크게 늘림 (120 → 160)
                BackColor = Color.FromArgb(0x46, 0x46, 0x52), // #464652
                BorderStyle = BorderStyle.None
            };
            resultSection.Region = CreateRoundedRegion(new Rectangle(0, 0, 460, 160), 10);
            // "Result" 라벨
            resultLabel = new Label
            {
                Text = "Result",
                Location = new Point(20, 12),
                Size = new Size(120, 28),
                Font = new Font(new FontFamily("Noto Sans KR"), 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xD9, 0xD9, 0xD9),
                BackColor = Color.Transparent,
                AutoSize = false
            };
            // 🎯 테스트 함수 실행 결과 표시 (TextBox로)
            var resultText = string.IsNullOrEmpty(testResult)
                ? "테스트 결과를 확인하고\nPASS 또는 FAIL을 선택해주세요."
                : testResult;
            //var bodyTextBox = new TextBox
            //{
            //    Text = resultText,
            //    Location = new Point(20, 48),
            //    Size = new Size(420, 110), // 높이를 늘림 (70 → 110)
            //    Font = new Font(new FontFamily("Noto Sans KR"), 10F, FontStyle.Regular), // 폰트 크기도 약간 줄임
            //    ForeColor = Color.White,
            //    BackColor = Color.FromArgb(0x46, 0x46, 0x52),
            //    BorderStyle = BorderStyle.None, // 테두리 제거해서 더 깔끔하게
            //    ReadOnly = true,
            //    Multiline = true,
            //    WordWrap = true, // ✅ 자동 줄바꿈 활성화
            //    ScrollBars = ScrollBars.None // ✅ 스크롤바 숨김 (마우스 휠로 스크롤 가능)
            //};
            var bodyTextBox = new RichTextBox
            {
                Text = resultText,
                Location = new Point(20, 48),
                Size = new Size(420, 80), // 높이를 늘림 (70 → 110)
                Font = new Font(new FontFamily("Noto Sans KR"), 10F, FontStyle.Regular), // 폰트 크기도 약간 줄임
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0x46, 0x46, 0x52),
                BorderStyle = BorderStyle.None, // 테두리 제거해서 더 깔끔하게
                ReadOnly = true,
                Multiline = true,
                WordWrap = true, // ✅ 자동 줄바꿈 활성화
                ScrollBars = RichTextBoxScrollBars.None, // ✅ 스크롤바 숨김 (마우스 휠로 스크롤 가능)
                DetectUrls = false
            };

            //var resultText2 = string.IsNullOrEmpty(res_param) ? "----" : res_param;
            //var bodyTextBox2 = new RichTextBox
            //{
            //    Text = resultText2,
            //    Location = new Point(20, 165),
            //    Size = new Size(420, 80), // 높이를 늘림 (70 → 110)
            //    Font = new Font(new FontFamily("Noto Sans KR"), 10F, FontStyle.Regular), // 폰트 크기도 약간 줄임
            //    ForeColor = Color.White,
            //    BackColor = Color.FromArgb(0x46, 0x46, 0x52),
            //    BorderStyle = BorderStyle.None, // 테두리 제거해서 더 깔끔하게
            //    ReadOnly = true,
            //    Multiline = true,
            //    WordWrap = true, // ✅ 자동 줄바꿈 활성화
            //    ScrollBars = RichTextBoxScrollBars.None, // ✅ 스크롤바 숨김 (마우스 휠로 스크롤 가능)
            //    DetectUrls = false
            //};

            resultSection.Controls.Add(resultLabel);
            resultSection.Controls.Add(bodyTextBox);
            //resultSection.Controls.Add(bodyTextBox2);
            dialogContent.Controls.Add(resultSection);

            bodyTextBox.LinkClicked += OnLinkClicked;
            HighlightLocalFilePaths(bodyTextBox);
        }
        private void HighlightLocalFilePaths(RichTextBox box)
        {
            var regex = new Regex(@"[a-zA-Z]:\\[^\s]+(?:\.png|\.jpg|\.jpeg|\.bmp|\.gif)", RegexOptions.IgnoreCase);
            var matches = regex.Matches(box.Text);

            foreach (Match match in matches)
            {
                int start = match.Index;
                int length = match.Length;

                box.Select(start, length);
                box.SetSelectionLink(true); // 링크로 설정
                box.SelectionColor = Color.DeepSkyBlue; // 링크 색상
                box.SelectionFont = new Font(box.Font, FontStyle.Underline); // 밑줄 강조
            }
            box.Select(0, 0); // 선택 해제
        }
        private bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".bmp" || ext == ".gif";
        }
        private void OnLinkClicked(object? sender, LinkClickedEventArgs e)
        {
            string path = e.LinkText.Trim('"');

            if (File.Exists(path) && IsImageFile(path))
            {
                try
                {
                    System.Diagnostics.Process.Start(new ProcessStartInfo
                    {
                        FileName = path,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("이미지를 열 수 없습니다: " + ex.Message);
                }
            }
            else
            {
                //MessageBox.Show("유효한 이미지 파일이 아닙니다.\n경로: " + path);
                Console.WriteLine("유효한 이미지 파일이 아닙니다.\n경로: " + path);
            }
        }

        // 🎯 라디오 버튼들 생성 (FAIL/PASS)
        private void CreateRadioButtons()
        {
            radioButtonsPanel = new Panel
            {
                Location = new Point(0, 255), // 안내문구 아래로 이동 (215 → 255)
                Size = new Size(460, 60), // 높이 컴팩트하게
                BackColor = Color.Transparent
            };

            failRadioButton = new CustomRadioButton
            {
                Location = new Point(100, 15), // 패널 높이에 맞춰 조정
                Size = new Size(120, 35),
                IsSelected = false // 기본값 PASS
            };
            failRadioButton.LabelText = "Fail";
            failRadioButton.ValueChanged += OnRadioButtonChanged_Fail;
            
            // 커스텀 라디오 버튼 생성
            passRadioButton = new CustomRadioButton
            {
                Location = new Point(240, 15), // 패널 높이에 맞춰 조정
                Size = new Size(120, 35),
                IsSelected = true // 기본값 PASS
            };
            passRadioButton.LabelText = "Pass";
            passRadioButton.ValueChanged += OnRadioButtonChanged_Pass;

            radioButtonsPanel.Controls.Add(failRadioButton);
            radioButtonsPanel.Controls.Add(passRadioButton);
            dialogContent.Controls.Add(radioButtonsPanel);
        }
        
        // 🎯 버튼들 생성 (Cancel/Save)
        private void CreateButtons()
        {
            buttonsPanel = new Panel
            {
                Location = new Point(0, 325), // 라디오 버튼 아래로 이동 (280 → 325)
                Size = new Size(460, 70), // 높이 약간 줄임
                BackColor = Color.Transparent
            };
            
            // Cancel 버튼 (2. Outlined)
            cancelButton = new Button
            {
                Location = new Point(40, 10),
                Size = new Size(160, 50), // 패널에 맞춰 조정
                Text = "Cancel",
                Font = new Font(new FontFamily("Noto Sans KR"), 14F, FontStyle.Regular), // 폰트 크기 조정
                ForeColor = Color.FromArgb(0xF3, 0x71, 0x21), // #F37121
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat
            };
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(0xF3, 0x71, 0x21);
            cancelButton.FlatAppearance.BorderSize = 3;
            cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 0, 0, 0); // 마우스 오버시 불투명한 검정색
            cancelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 0, 0, 0); // 클릭시 더 진한 불투명한 검정색
            cancelButton.Click += OnCancelClick;
            
            cancelButton.Region = CreateRoundedRegion(new Rectangle(0, 0, 160, 50), 16);
            
            // Save 버튼 (3. Contained)
            saveButton = new Button
            {
                Location = new Point(260, 10),
                Size = new Size(160, 50), // 패널에 맞춰 조정
                Text = "Save",
                Font = new Font(new FontFamily("Noto Sans KR"), 14F, FontStyle.Regular), // 폰트 크기 조정
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D), // #18181D
                BackColor = Color.FromArgb(0xF3, 0x71, 0x21), // #F37121
                FlatStyle = FlatStyle.Flat
            };
            saveButton.FlatAppearance.BorderColor = Color.FromArgb(0xF3, 0x71, 0x21);
            saveButton.FlatAppearance.BorderSize = 1;

            saveButton.Region = CreateRoundedRegion(new Rectangle(0, 0, 160, 50), 16);
            
            saveButton.Click += OnSaveClick;
            
            buttonsPanel.Controls.Add(cancelButton);
            buttonsPanel.Controls.Add(saveButton);
            dialogContent.Controls.Add(buttonsPanel); // ✅ dialogContent에 추가해야 함
        }
        
        // 🎯 라디오 버튼 변경 이벤트
        private void OnRadioButtonChanged_Pass(object? sender, bool value)
        {
            passRadioButton.SetIsSelected(value);
            failRadioButton.SetIsSelected(!value);
        }
        
        private void OnRadioButtonChanged_Fail(object? sender, bool value)
        {
            passRadioButton.SetIsSelected(!value);
            failRadioButton.SetIsSelected(value);
        }
        
        // 🎯 Cancel 버튼 클릭
        private void OnCancelClick(object? sender, EventArgs e)
        {
            DialogConfirmed = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
        
        // 🎯 Save 버튼 클릭
        private void OnSaveClick(object? sender, EventArgs e)
        {
            DialogConfirmed = true;
            TestResult = passRadioButton.IsSelected;// 기본값 PASS
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        // 🎯 키보드 이벤트 처리
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                OnCancelClick(null, null);
                return true;
            }
            else if (keyData == Keys.Enter)
            {
                OnSaveClick(null, null);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        
        // 🎯 폼 외부 클릭 시 닫기
        // protected override void OnDeactivate(EventArgs e)
        // {
        //     base.OnDeactivate(e);
        //     // 폼이 비활성화되면 취소로 처리
        //     if (this.Visible)
        //     {
        //         OnCancelClick(null, null);
        //     }
        // }
    }
} 