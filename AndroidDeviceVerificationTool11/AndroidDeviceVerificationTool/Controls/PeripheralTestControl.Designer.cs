namespace AndroidDeviceVerificationTool.Controls
{
    partial class PeripheralTestControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            // 🏗️ 디바이스 영역만 (각 디바이스에 개별 로그 포함)
            devicePanel = new Panel();
            
            SuspendLayout();
            
            // Main UserControl
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(47, 47, 55); // #2F2F37
            Size = new Size(1366, 704);
            Font = new Font("맑은 고딕", 9F, FontStyle.Regular, GraphicsUnit.Point);
            
            // 🎮 Device Panel (전체 영역 - 각 디바이스에 개별 로그 포함)
            devicePanel.BackColor = Color.FromArgb(47, 47, 55);
            devicePanel.Dock = DockStyle.Fill;
            devicePanel.Location = new Point(0, 0);
            devicePanel.Name = "devicePanel";
            devicePanel.Size = new Size(1366, 704);  // 전체 높이 사용
            devicePanel.TabIndex = 0;
            
            // Add panel to main control
            Controls.Add(devicePanel);
            
            ResumeLayout(false);
        }

        #endregion

        private Panel devicePanel;
    }
} 