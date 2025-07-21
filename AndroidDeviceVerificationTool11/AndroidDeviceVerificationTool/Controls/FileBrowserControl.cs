using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AndroidDeviceVerificationTool.Controls
{
    public partial class DarkFileBrowserDialog : Form
    {
        // 🎨 색상 정의
        private static readonly Color BACKGROUND = Color.FromArgb(0x2A, 0x2D, 0x38);
        private static readonly Color TEXT_COLOR = Color.FromArgb(0xF8, 0xF9, 0xFA);
        private static readonly Color INPUT_BG = Color.FromArgb(0x21, 0x24, 0x2F);
        private static readonly Color BUTTON_BG = Color.FromArgb(0x57, 0x57, 0x65); // #0D6EFD
        private static readonly Color SUCCESS_BG = Color.FromArgb(0x28, 0xA7, 0x45);
        private static readonly Color CANCEL_BG = Color.FromArgb(0x6C, 0x75, 0x7D);

        // 컨트롤들
        private TextBox filePathTextBox;
        private Button browseButton;
        private Button saveButton;
        private Button cancelButton;
        private Button clearCacheButton;
        private Label titleLabel;
        private Label pathLabel;

        // 프로퍼티
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SelectedFilePath { get; private set; } = "";
        
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string FileFilter { get; set; } = "All Files (*.*)|*.*";
        
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DialogTitle { get; set; } = "File Browser";

        public DarkFileBrowserDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // 다이얼로그 기본 설정
            this.Text = "File Browser";
            this.Size = new Size(600, 250);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = BACKGROUND;
            this.Font = new Font("Segoe UI", 10F);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            CreateControls();
        }
        
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

        private void CreateControls()
        {
            // 제목
            titleLabel = new Label
            {
                Text = "📁 Json Setting File",
                Location = new Point(20, 20),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = TEXT_COLOR,
                BackColor = Color.Transparent
            };

            // 파일 경로 라벨
            pathLabel = new Label
            {
                Text = "File Path:",
                Location = new Point(20, 60),
                Size = new Size(100, 20),
                Font = new Font("Segoe UI", 10F),
                ForeColor = TEXT_COLOR,
                BackColor = Color.Transparent
            };

            // 파일 경로 텍스트박스
            filePathTextBox = new TextBox
            {
                Location = new Point(20, 85),
                Size = new Size(450, 25),
                Font = new Font("Segoe UI", 10F),
                BackColor = INPUT_BG,
                ForeColor = TEXT_COLOR,
                BorderStyle = BorderStyle.FixedSingle,
                Text = ""
            };

            // Browse 버튼 (파일 불러오기)
            browseButton = new Button
            {
                Text = "Browse",
                Location = new Point(480, 82),
                Size = new Size(80, 30),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = BUTTON_BG,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            browseButton.FlatAppearance.BorderSize = 0;
            browseButton.Click += BrowseButton_Click;

            // Clear Cache 버튼 (캐시 지우기)
            clearCacheButton = new Button
            {
                Text = "Clear Cache",
                Location = new Point(270, 160),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(0xDC, 0x35, 0x45), // #DC3545 (빨간색)
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            clearCacheButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 0xDC, 0x35, 0x45); // 마우스 오버시 반투명 빨간색
            clearCacheButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 0xDC, 0x35, 0x45); // 클릭시 더 진한 반투명 빨간색
            clearCacheButton.FlatAppearance.BorderColor = Color.FromArgb(0xDC, 0x35, 0x45);
            clearCacheButton.FlatAppearance.BorderSize = 2;
            clearCacheButton.Click += ClearCacheButton_Click;
            clearCacheButton.Region = CreateRoundedRegion(new Rectangle(0, 0, 100, 35), 8);

            // Cancel 버튼 (취소하기)
            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(380, 160),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0xF3, 0x71, 0x21), // #F37121
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            cancelButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 0, 0, 0); // 마우스 오버시 불투명한 검정색
            cancelButton.FlatAppearance.MouseDownBackColor = Color.FromArgb(80, 0, 0, 0); // 클릭시 더 진한 불투명한 검정색
            cancelButton.FlatAppearance.BorderColor = Color.FromArgb(0xF3, 0x71, 0x21);
            cancelButton.FlatAppearance.BorderSize = 2;
            cancelButton.Click += CancelButton_Click;
            
            cancelButton.Region = CreateRoundedRegion(new Rectangle(0, 0, 80, 35), 8);

            // Save 버튼 (파일 저장하기)
            saveButton = new Button
            {
                Text = "Save",
                Location = new Point(480, 160),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0x18, 0x18, 0x1D), // #18181D
                BackColor = Color.FromArgb(0xF3, 0x71, 0x21), // #F37121
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };

            saveButton.Region = CreateRoundedRegion(new Rectangle(0, 0, 80, 35), 8);
            
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += SaveButton_Click;

            // 이벤트 추가
            filePathTextBox.KeyDown += FilePathTextBox_KeyDown;
            filePathTextBox.TextChanged += FilePathTextBox_TextChanged;

            // 컨트롤 추가
            this.Controls.Add(titleLabel);
            this.Controls.Add(pathLabel);
            this.Controls.Add(filePathTextBox);
            this.Controls.Add(browseButton);
            this.Controls.Add(clearCacheButton);
            this.Controls.Add(cancelButton);
            this.Controls.Add(saveButton);

            // 버튼 설정
            this.AcceptButton = saveButton;
            this.CancelButton = cancelButton;
        }

        // Browse 버튼 클릭 (파일 불러오기)
        private void BrowseButton_Click(object sender, EventArgs e)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = FileFilter;
                openFileDialog.Title = "Select File";
                openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                if (openFileDialog.ShowDialog(this.FindForm()) == DialogResult.OK)
                {
                    filePathTextBox.Text = openFileDialog.FileName;
                    SelectedFilePath = openFileDialog.FileName;
                }
            }
        }

        // 텍스트박스 키 이벤트 (파일 경로 입력)
        private void FilePathTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                string filePath = filePathTextBox.Text.Trim();
                if (!string.IsNullOrEmpty(filePath))
                {
                    SelectedFilePath = filePath;
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                e.Handled = true;
            }
        }

        // 텍스트박스 내용 변경
        private void FilePathTextBox_TextChanged(object sender, EventArgs e)
        {
            // 텍스트가 있으면 Save 버튼 활성화
            bool hasText = !string.IsNullOrWhiteSpace(filePathTextBox.Text);
            saveButton.Enabled = hasText;
            saveButton.BackColor = hasText ? SUCCESS_BG : Color.Gray;
        }

        // Save 버튼 클릭 (파일 저장하기)
        private void SaveButton_Click(object sender, EventArgs e)
        {
            string filePath = filePathTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(filePath))
            {
                SelectedFilePath = filePath;
                this.DialogResult = DialogResult.OK;
            }
            else
            {
                MessageBox.Show(this.FindForm(), "파일 경로를 입력해주세요.", "입력 오류", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Cancel 버튼 클릭 (취소하기)
        private void CancelButton_Click(object sender, EventArgs e)
        {
            SelectedFilePath = "";
            this.DialogResult = DialogResult.Cancel;
        }

        // Clear Cache 버튼 클릭 (캐시 지우기)
        private void ClearCacheButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 확인 다이얼로그 표시
                var result = MessageBox.Show(this.FindForm(), 
                    "마지막으로 사용한 설정 파일 정보를 삭제하시겠습니까?\n\n다음 실행시 기본 설정으로 시작됩니다.", 
                    "캐시 삭제 확인", 
                    MessageBoxButtons.YesNo, 
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Config 클래스를 통해 캐시 삭제
                    bool success = AndroidDeviceVerificationTool.Data.Config.Instance.ClearLastUsedConfigPath();
                    
                    if (success)
                    {
                        MessageBox.Show(this.FindForm(), 
                            "✅ 캐시가 성공적으로 삭제되었습니다.\n\n다음 실행시 기본 설정으로 시작됩니다.", 
                            "캐시 삭제 완료", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show(this.FindForm(), 
                            "⚠️ 캐시 삭제 중 문제가 발생했습니다.\n권한을 확인해주세요.", 
                            "캐시 삭제 실패", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(this.FindForm(), 
                    $"❌ 캐시 삭제 중 오류가 발생했습니다:\n{ex.Message}", 
                    "오류", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        // 정적 메서드로 쉽게 사용할 수 있도록
        public static string ShowBrowserDialog(Form parent, string title = "File Browser", string filter = "All Files (*.*)|*.*")
        {
            using (var dialog = new DarkFileBrowserDialog())
            {
                dialog.Text = title;
                dialog.DialogTitle = title;
                dialog.FileFilter = filter;
                
                var result = dialog.ShowDialog(parent);
                return result == DialogResult.OK ? dialog.SelectedFilePath : null;
            }
        }
    }
} 