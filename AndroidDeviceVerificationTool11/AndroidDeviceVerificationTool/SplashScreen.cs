using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace AndroidDeviceVerificationTool
{
    public partial class SplashScreen : Form
    {
        private System.Windows.Forms.Timer fadeTimer;
        private bool isClosing = false;
        private double opacity = 0.0;
        private Image? logoImage;

        public SplashScreen()
        {
            this.Opacity = 0.0; // 시작할 때 투명
            LoadLogo();
            SetupSplashScreen();
        }

        private void LoadLogo()
        {
            try
            {
                // 여러 위치에서 로고 파일 찾기
                string[] possiblePaths = {
                    Path.Combine(Application.StartupPath, "Resources", "logo.png"),
                    Path.Combine(Application.StartupPath, "logo.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "Resources", "logo.png"),
                    Path.Combine(Directory.GetCurrentDirectory(), "logo.png"),
                    "Resources/logo.png",
                    "logo.png"
                };

                foreach (string logoPath in possiblePaths)
                {
                    if (File.Exists(logoPath))
                    {
                        logoImage = Image.FromFile(logoPath);
                        break;
                    }
                }
            }
            catch
            {
                // 로고 로드 실패 시 무시
            }
        }

        private void SetupSplashScreen()
        {
            // 폼 기본 설정 - 큰 창
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.FromArgb(0x2F, 0x2F, 0x37); // 다크 그레이 배경
            this.ShowInTaskbar = false;
            this.TopMost = true;

            // 더블 버퍼링과 블러 효과를 위한 설정
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint | 
                         ControlStyles.DoubleBuffer | 
                         ControlStyles.ResizeRedraw, true);

            // Load 이벤트에서 페이드인 시작
            this.Load += (s, e) => StartFadeInAnimation();
            
            // FormClosed 이벤트에서 리소스 정리
            this.FormClosed += (s, e) => 
            {
                fadeTimer?.Stop();
                fadeTimer?.Dispose();
                logoImage?.Dispose();
            };
        }

        // 페이드 인 애니메이션 시작
        private void StartFadeInAnimation()
        {
            fadeTimer = new System.Windows.Forms.Timer();
            fadeTimer.Interval = 50; // 50ms마다 업데이트
            fadeTimer.Tick += FadeTimer_Tick;
            fadeTimer.Start();

            // 3초 후 자동 종료
            Task.Run(async () =>
            {
                await Task.Delay(3000);
                if (!this.IsDisposed)
                {
                    this.Invoke(new Action(() => StartFadeOut()));
                }
            });
        }

        // 페이드 애니메이션 처리
        private void FadeTimer_Tick(object sender, EventArgs e)
        {
            if (!isClosing)
            {
                // 페이드 인
                if (opacity < 1.0)
                {
                    opacity += 0.05;
                    this.Opacity = Math.Min(opacity, 1.0);
                }
            }
            else
            {
                // 페이드 아웃
                if (opacity > 0.0)
                {
                    opacity -= 0.1;
                    this.Opacity = Math.Max(opacity, 0.0);
                }
                else
                {
                    fadeTimer?.Stop();
                    this.Close();
                }
            }
        }

        // 페이드 아웃 시작
        public void StartFadeOut()
        {
            isClosing = true;
        }

        // 폼 클릭 시 바로 종료
        protected override void OnClick(EventArgs e)
        {
            StartFadeOut();
            base.OnClick(e);
        }

        // 키 입력 시 바로 종료
        protected override void OnKeyDown(KeyEventArgs e)
        {
            StartFadeOut();
            base.OnKeyDown(e);
        }

        // 디자인에 맞는 그리기
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            // 다크 그레이 블러 배경
            using (var brush = new LinearGradientBrush(
                new Point(0, 0), 
                new Point(this.Width, this.Height), 
                Color.FromArgb(0x2F, 0x2F, 0x37), 
                Color.FromArgb(0x1A, 0x1A, 0x1A)))
            {
                g.FillRectangle(brush, 0, 0, this.Width, this.Height);
            }

            // 블러 효과를 위한 추가 레이어들
            using (var brush1 = new LinearGradientBrush(
                new Point(0, 0), 
                new Point(this.Width / 2, this.Height / 2), 
                Color.FromArgb(30, 0x46, 0x46, 0x52), 
                Color.Transparent))
            {
                g.FillRectangle(brush1, 0, 0, this.Width, this.Height);
            }

            using (var brush2 = new LinearGradientBrush(
                new Point(this.Width, 0), 
                new Point(this.Width / 2, this.Height), 
                Color.FromArgb(20, 0x3A, 0x3A, 0x42), 
                Color.Transparent))
            {
                g.FillRectangle(brush2, 0, 0, this.Width, this.Height);
            }

            // 좌상단에 로고 그리기
            if (logoImage != null)
            {
                int logoX = 30; // 좌상단 여백
                int logoY = 30;
                
                // 원본 사이즈 사용 (너무 클 경우 최대 크기 제한)
                int maxLogoSize = 150;
                int logoWidth = logoImage.Width;
                int logoHeight = logoImage.Height;
                
                // 이미지가 너무 클 경우 비율을 유지하면서 크기 조정
                if (logoWidth > maxLogoSize || logoHeight > maxLogoSize)
                {
                    float scale = Math.Min((float)maxLogoSize / logoWidth, (float)maxLogoSize / logoHeight);
                    logoWidth = (int)(logoWidth * scale);
                    logoHeight = (int)(logoHeight * scale);
                }
                
                g.DrawImage(logoImage, logoX, logoY, logoWidth, logoHeight);
            }

            // 중앙에 메인 타이틀
            using (var font = new Font("Segoe UI", 36F, FontStyle.Bold))
            using (var brush = new SolidBrush(Color.White))
            {
                var text = "Device Test Tool";
                var textSize = g.MeasureString(text, font);
                var textX = (this.Width - textSize.Width) / 2;
                var textY = (this.Height - textSize.Height) / 2;

                // 텍스트 그림자 (블러 효과)
                using (var shadowBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0)))
                {
                    g.DrawString(text, font, shadowBrush, textX + 2, textY + 2);
                }

                // 메인 텍스트
                g.DrawString(text, font, brush, textX, textY);
            }

            // 하단에 저작권 표시
            using (var font = new Font("Segoe UI", 10F))
            using (var brush = new SolidBrush(Color.FromArgb(180, 180, 180)))
            {
                var text = "Copyright © 2025. ST SUNLAB Ltd. All rights reserved.";
                var textSize = g.MeasureString(text, font);
                var textX = (this.Width - textSize.Width) / 2;
                var textY = this.Height - 60; // 하단에서 60px 위

                g.DrawString(text, font, brush, textX, textY);
            }

            // 테두리 (선택사항)
            using (var pen = new Pen(Color.FromArgb(80, 0x46, 0x46, 0x52), 2))
            {
                g.DrawRectangle(pen, 1, 1, this.Width - 3, this.Height - 3);
            }
        }
    }
} 