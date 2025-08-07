using System.Drawing.Printing;

namespace AndroidDeviceVerificationTool;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.titlePanel = new Panel();
        this.titleLabel = new Label();
        this.minimizeButton = new Button();
        //this.maximizeButton = new Button();
        this.closeButton = new Button();
        this.testConnectionButton = new Button();

        this.testAdbCmdButton = new Button();
        this.testDbButton = new Button();

        this.modelNameTextBox = new TextBox();
        this.settingsButton = new Button();
        this.tabButtonsPanel = new Panel();
        this.peripheralTestButton = new Button();
        this.micInWriteButton = new Button();
        this.postSetupButton = new Button();
        this.contentPanel = new Panel();
        this.peripheralTestPanel = new Panel();
        this.micInWritePanel = new Panel();
        this.postSetupPanel = new Panel();
        
        this.titlePanel.SuspendLayout();
        this.tabButtonsPanel.SuspendLayout();
        this.contentPanel.SuspendLayout();
        this.SuspendLayout();
        
        // Form
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1600, 900);
        this.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
        this.Text = "Device Test Tool (1600x900) Ver:1.0";
        this.Font = new Font("Noto Sans KR", 9F, FontStyle.Regular, GraphicsUnit.Point);
        this.FormBorderStyle = FormBorderStyle.None;
        this.ForeColor = Color.FromArgb(0x18, 0x18, 0x1D); // #18181D
        this.StartPosition = FormStartPosition.CenterScreen;
        
        // Title Panel (오렌지색 타이틀바) - 더블버퍼링 적용
        this.titlePanel.Location = new Point(0, 0);
        this.titlePanel.Size = new Size(1600, 40);
        this.titlePanel.BackColor = Color.FromArgb(243, 113, 33);
        this.titlePanel.Controls.Add(this.titleLabel);
#if false        
        this.titlePanel.Controls.Add(this.testConnectionButton);
        this.titlePanel.Controls.Add(this.testAdbCmdButton); 
        this.titlePanel.Controls.Add(this.testDbButton); 
#endif
        this.titlePanel.Controls.Add(this.modelNameTextBox);
        this.titlePanel.Controls.Add(this.minimizeButton);
        //this.titlePanel.Controls.Add(this.maximizeButton);
        this.titlePanel.Controls.Add(this.settingsButton);
        this.titlePanel.Controls.Add(this.closeButton);
        this.titlePanel.MouseDown += TitlePanel_MouseDown;
        typeof(Panel).InvokeMember("DoubleBuffered", 
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, this.titlePanel, new object[] { true });
        
        // Title Label
        this.titleLabel.Text = "Device Test Tool Ver:1.0";
        this.titleLabel.Location = new Point(10, 10);
        this.titleLabel.Size = new Size(150, 20);
        this.titleLabel.ForeColor = Color.Black;
        this.titleLabel.Font = new Font("Noto Sans KR", 11F, FontStyle.Bold);
        this.titleLabel.TextAlign = ContentAlignment.MiddleLeft;

        // Window Control Buttons
        this.closeButton.Text = "✕";
        this.closeButton.Location = new Point(1565, 5);
        this.closeButton.Size = new Size(30, 30);
        this.closeButton.BackColor = Color.Transparent;
        this.closeButton.ForeColor = Color.White;
        this.closeButton.FlatStyle = FlatStyle.Flat;
        this.closeButton.FlatAppearance.BorderSize = 0;
        this.closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 53, 69);
        this.closeButton.Font = new Font("Arial", 10F, FontStyle.Bold);
        this.closeButton.Click += CloseButton_Click;
        
        //this.maximizeButton.Text = "□";
        //this.maximizeButton.Location = new Point(1530, 5);
        //this.maximizeButton.Size = new Size(30, 30);
        //this.maximizeButton.BackColor = Color.Transparent;
        //this.maximizeButton.ForeColor = Color.White;
        //this.maximizeButton.FlatStyle = FlatStyle.Flat;
        //this.maximizeButton.FlatAppearance.BorderSize = 0;
        //this.maximizeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 113, 33);
        //this.maximizeButton.Font = new Font("Arial", 10F);
        //this.maximizeButton.Click += MaximizeButton_Click;
        
        this.minimizeButton.Text = "─";
        this.minimizeButton.Location = new Point(1530, 5);  //1495
        this.minimizeButton.Size = new Size(30, 30);
        this.minimizeButton.BackColor = Color.Transparent;
        this.minimizeButton.ForeColor = Color.White;
        this.minimizeButton.FlatStyle = FlatStyle.Flat;
        this.minimizeButton.FlatAppearance.BorderSize = 0;
        this.minimizeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 113, 33);
        this.minimizeButton.Font = new Font("Arial", 10F, FontStyle.Bold);
        this.minimizeButton.Click += MinimizeButton_Click;

        this.settingsButton.Text = "\u2699\ufe0f";
        this.settingsButton.Location = new Point(1495, 5);  //1465
        this.settingsButton.Size = new Size(30, 30);
        this.settingsButton.BackColor = Color.Transparent;
        this.settingsButton.ForeColor = Color.White;
        this.settingsButton.FlatStyle = FlatStyle.Flat;
        this.settingsButton.FlatAppearance.BorderSize = 0;
        this.settingsButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 113, 33);
        this.settingsButton.Font = new Font("Arial", 10F, FontStyle.Bold);
        this.settingsButton.Click += OpenFileBrowser_Click;

        this.modelNameTextBox.Text = "N/A";
        this.modelNameTextBox.Location = new Point(1250, 8);
        this.modelNameTextBox.Size = new Size(200, 24);
        this.modelNameTextBox.BackColor = Color.FromArgb(220, 93, 13);
        this.modelNameTextBox.ForeColor = Color.White;
        this.modelNameTextBox.Font = new Font("Noto Sans KR", 10F, FontStyle.Regular);
        this.modelNameTextBox.BorderStyle = BorderStyle.FixedSingle;
        this.modelNameTextBox.TextAlign = HorizontalAlignment.Center;
        this.modelNameTextBox.TextChanged += ModelNameTextBox_TextChanged;
        this.modelNameTextBox.ReadOnly = true;
        this.modelNameTextBox.TabStop = false;

        this.testConnectionButton.Text = "Test";
        this.testConnectionButton.Location = new Point(200, 5);
        this.testConnectionButton.Size = new Size(100, 30);
        this.testConnectionButton.BackColor = Color.FromArgb(220, 93, 13);
        this.testConnectionButton.ForeColor = Color.White;
        this.testConnectionButton.FlatStyle = FlatStyle.Flat;
        this.testConnectionButton.FlatAppearance.BorderSize = 0;
        this.testConnectionButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 73, 0);
        this.testConnectionButton.Font = new Font("Noto Sans KR", 9F, FontStyle.Regular);
        this.testConnectionButton.Click += TestConnectionButton_Click;

        this.testAdbCmdButton.Text = "ADB";
        this.testAdbCmdButton.Location = new Point(300, 5);
        this.testAdbCmdButton.Size = new Size(100, 30);
        this.testAdbCmdButton.BackColor = Color.FromArgb(220, 93, 13);
        this.testAdbCmdButton.ForeColor = Color.White;
        this.testAdbCmdButton.FlatStyle = FlatStyle.Flat;
        this.testAdbCmdButton.FlatAppearance.BorderSize = 0;
        this.testAdbCmdButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 73, 0);
        this.testAdbCmdButton.Font = new Font("Noto Sans KR", 9F, FontStyle.Regular);
        this.testAdbCmdButton.Click += TestAdbCmdButton_Click;

        
        this.testDbButton.Text = "DB";
        this.testDbButton.Location = new Point(400, 5);
        this.testDbButton.Size = new Size(100, 30);
        this.testDbButton.BackColor = Color.FromArgb(220, 93, 13);
        this.testDbButton.ForeColor = Color.White;
        this.testDbButton.FlatStyle = FlatStyle.Flat;
        this.testDbButton.FlatAppearance.BorderSize = 0;
        this.testDbButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(200, 73, 0);
        this.testDbButton.Font = new Font("Noto Sans KR", 9F, FontStyle.Regular);
        this.testDbButton.Click += TestDbButton_Click;

        // Tab Buttons Panel (커스텀 탭 버튼들) - 더블버퍼링 적용
        this.tabButtonsPanel.Location = new Point(0, 40);
        this.tabButtonsPanel.Size = new Size(1600, 50);
        this.tabButtonsPanel.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
        this.tabButtonsPanel.BorderStyle = BorderStyle.None;
        typeof(Panel).InvokeMember("DoubleBuffered", 
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, this.tabButtonsPanel, new object[] { true });
        
        // Peripheral Test Button
        this.peripheralTestButton.Text = "Peripheral Test"; 
        this.peripheralTestButton.Location = new Point(0, 0);
        this.peripheralTestButton.Size = new Size(200, 50);
        this.peripheralTestButton.BackColor = Color.FromArgb(87, 87, 101); // 선택된 탭 색상
        this.peripheralTestButton.ForeColor = Color.White;
        this.peripheralTestButton.FlatStyle = FlatStyle.Flat;
        this.peripheralTestButton.FlatAppearance.BorderSize = 0;
        this.peripheralTestButton.Font = new Font("Noto Sans KR", 10F, FontStyle.Regular);
        this.peripheralTestButton.Click += PeripheralTestButton_Click;
        this.peripheralTestButton.Tag = "selected"; // 기본 선택
        
        // IMEI/SN Write Button
        this.micInWriteButton.Text = "IMEI/SN Write";
        this.micInWriteButton.Location = new Point(200, 0);
        this.micInWriteButton.Size = new Size(200, 50);
        this.micInWriteButton.BackColor = Color.FromArgb(67, 67, 75); // 비선택 탭 색상
        this.micInWriteButton.ForeColor = Color.FromArgb(200, 200, 200);
        this.micInWriteButton.FlatStyle = FlatStyle.Flat;
        this.micInWriteButton.FlatAppearance.BorderSize = 0;
        this.micInWriteButton.Font = new Font("Noto Sans KR", 10F, FontStyle.Regular);
        this.micInWriteButton.Click += MicInWriteButton_Click;
        
        // Post Setup Button
        this.postSetupButton.Text = "Post Setup";
        this.postSetupButton.Location = new Point(400, 0);
        this.postSetupButton.Size = new Size(200, 50);
        this.postSetupButton.BackColor = Color.FromArgb(67, 67, 75); // 비선택 탭 색상
        this.postSetupButton.ForeColor = Color.FromArgb(200, 200, 200);
        this.postSetupButton.FlatStyle = FlatStyle.Flat;
        this.postSetupButton.FlatAppearance.BorderSize = 0;
        this.postSetupButton.Font = new Font("Noto Sans KR", 10F, FontStyle.Regular);
        this.postSetupButton.Click += PostSetupButton_Click;
        

        
        this.tabButtonsPanel.Controls.Add(this.peripheralTestButton);
        this.tabButtonsPanel.Controls.Add(this.micInWriteButton);
        this.tabButtonsPanel.Controls.Add(this.postSetupButton);
        
        // Content Panel (탭 컨텐츠 영역) - 더블버퍼링 적용
        this.contentPanel.Location = new Point(0, 90);
        this.contentPanel.Size = new Size(1600, 810);
        this.contentPanel.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
        this.contentPanel.BorderStyle = BorderStyle.None;
        typeof(Panel).InvokeMember("DoubleBuffered", 
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, this.contentPanel, new object[] { true });
        
        // Peripheral Test Panel - 더블버퍼링 적용
        this.peripheralTestPanel.Dock = DockStyle.Fill;
        this.peripheralTestPanel.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
        this.peripheralTestPanel.Visible = true; // 기본 선택
        typeof(Panel).InvokeMember("DoubleBuffered", 
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, this.peripheralTestPanel, new object[] { true });
        
        // MIC/IN Write Panel - 더블버퍼링 임시 비활성화
        this.micInWritePanel.Dock = DockStyle.Fill;
        this.micInWritePanel.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
        this.micInWritePanel.Visible = false;
        // typeof(Panel).InvokeMember("DoubleBuffered", 
        //     System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
        //     null, this.micInWritePanel, new object[] { true });
        
        // Post Setup Panel - 더블버퍼링 임시 비활성화
        this.postSetupPanel.Dock = DockStyle.Fill;
        this.postSetupPanel.BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
        this.postSetupPanel.Visible = false;
        // typeof(Panel).InvokeMember("DoubleBuffered", 
        //     System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
        //     null, this.postSetupPanel, new object[] { true });
        

        
        this.contentPanel.Controls.Add(this.peripheralTestPanel);
        this.contentPanel.Controls.Add(this.micInWritePanel);
        this.contentPanel.Controls.Add(this.postSetupPanel);
        
        // Form Controls
        this.Controls.Add(this.titlePanel);
        this.Controls.Add(this.tabButtonsPanel);
        this.Controls.Add(this.contentPanel);
        
        this.titlePanel.ResumeLayout(false);
        this.tabButtonsPanel.ResumeLayout(false);
        this.contentPanel.ResumeLayout(false);
        this.ResumeLayout(false);
    }

    #endregion

    private Panel titlePanel;
    private Label titleLabel;
    private Button minimizeButton;
    //private Button maximizeButton;
    private Button closeButton;
    private Button testConnectionButton;
    private TextBox modelNameTextBox;
    private Button settingsButton;
    private Panel tabButtonsPanel;
    private Button peripheralTestButton;
    private Button micInWriteButton;
    private Button postSetupButton;
    private Panel contentPanel;
    private Panel peripheralTestPanel;
    private Panel micInWritePanel;
    private Panel postSetupPanel;


    private Button testAdbCmdButton;
    private Button testDbButton;


    // Window control event handlers
    private void CloseButton_Click(object sender, EventArgs e)
    {
        this.Close();
    }
    
    //private void MaximizeButton_Click(object sender, EventArgs e)
    //{
    //    if (this.WindowState == FormWindowState.Maximized)
    //    {
    //        this.WindowState = FormWindowState.Normal;
    //        this.maximizeButton.Text = "□";
    //    }
    //    else
    //    {
    //        this.WindowState = FormWindowState.Maximized;
    //        this.maximizeButton.Text = "❐";
    //    }
    //}
    
    private void MinimizeButton_Click(object sender, EventArgs e)
    {
        this.WindowState = FormWindowState.Minimized;
    }
    
    // Custom Tab Button Click Handlers
    private void PeripheralTestButton_Click(object sender, EventArgs e)
    {
        SwitchTab(0);
    }
    
    private void MicInWriteButton_Click(object sender, EventArgs e)
    {
        SwitchTab(1);
    }
    
    private void PostSetupButton_Click(object sender, EventArgs e)
    {
        SwitchTab(2);
    }
    
    private void SwitchTab(int tabIndex)
    {
        // 🔍 **디버그: 탭 전환 시작**
        // 임시로 간단한 방식으로 변경해서 테스트
        
        // 🏭 Production Mode로 복원 - Communication 버튼들 숨김
        ((Form1)this.FindForm())?.HideTestModeButtons();
        
        // 모든 패널 숨기기
        peripheralTestPanel.Visible = false;
        micInWritePanel.Visible = false;
        postSetupPanel.Visible = false;
        
        // 모든 버튼을 비선택 상태로
        peripheralTestButton.BackColor = Color.FromArgb(67, 67, 75);
        peripheralTestButton.ForeColor = Color.FromArgb(200, 200, 200);
        peripheralTestButton.Tag = null;
        
        micInWriteButton.BackColor = Color.FromArgb(67, 67, 75);
        micInWriteButton.ForeColor = Color.FromArgb(200, 200, 200);
        micInWriteButton.Tag = null;
        
        postSetupButton.BackColor = Color.FromArgb(67, 67, 75);
        postSetupButton.ForeColor = Color.FromArgb(200, 200, 200);
        postSetupButton.Tag = null;
        
        // 선택된 탭 표시
        switch (tabIndex)
        {
            case 0:
                peripheralTestPanel.Visible = true;
                peripheralTestButton.BackColor = Color.FromArgb(87, 87, 101);
                peripheralTestButton.ForeColor = Color.White;
                peripheralTestButton.Tag = "selected";
                break;
                            case 1:
                    micInWritePanel.Visible = true;
                    micInWriteButton.BackColor = Color.FromArgb(87, 87, 101);
                    micInWriteButton.ForeColor = Color.White;
                    micInWriteButton.Tag = "selected";
                    // 🔍 **강제 새로고침으로 테스트**
                    micInWritePanel.Refresh();
                    micInWritePanel.Invalidate();
                    // 🔍 **컨트롤에 포커스 주기**
                    if (micInWritePanel.Controls.Count > 0)
                    {
                        micInWritePanel.Controls[0].Focus();
                    }
                    break;
                case 2:
                    postSetupPanel.Visible = true;
                    postSetupButton.BackColor = Color.FromArgb(87, 87, 101);
                    postSetupButton.ForeColor = Color.White;
                    postSetupButton.Tag = "selected";
                    // 🔍 **강제 새로고침으로 테스트**
                    postSetupPanel.Refresh();
                    postSetupPanel.Invalidate();
                    // 🔍 **컨트롤에 포커스 주기**
                    if (postSetupPanel.Controls.Count > 0)
                    {
                        postSetupPanel.Controls[0].Focus();
                    }
                    break;
        }
        
        // 🔍 **전체 컨텐츠 패널 새로고침**
        this.contentPanel.Refresh();
    }
    
    // Title panel mouse handling for window dragging
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;
    
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    
    private void TitlePanel_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        }
    }
    
    private void ModelNameTextBox_TextChanged(object sender, EventArgs e)
    {
        if (sender is TextBox textBox)
        {
            // SharedApplicationData를 통해 모든 UI에 브로드캐스팅
            var sharedData = AndroidDeviceVerificationTool.Data.SharedApplicationData.Instance;
            sharedData.SetModelName(textBox.Text);
        }
    }
}
