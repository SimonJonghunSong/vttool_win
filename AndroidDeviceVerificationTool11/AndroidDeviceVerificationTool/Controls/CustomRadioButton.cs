using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.ComponentModel;

namespace AndroidDeviceVerificationTool.Controls
{
    public partial class CustomRadioButton : UserControl
    {
        private bool isSelected = false;
        private string labelText = "Radio Button";
        private Color borderColor = Color.FromArgb(0xF3, 0x71, 0x21); // 주황색
        private Color selectedColor = Color.FromArgb(0xF3, 0x71, 0x21); // 주황색
        private Color textColor = Color.White;

        public event EventHandler<bool> ValueChanged;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    Invalidate(); // 다시 그리기
                    ValueChanged?.Invoke(this, isSelected);
                }
            }
        }

        public void SetIsSelected(bool selected)
        {
            isSelected = selected;
            Invalidate();
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string LabelText
        {
            get => labelText;
            set
            {
                labelText = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color BorderColor
        {
            get => borderColor;
            set
            {
                borderColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color SelectedColor
        {
            get => selectedColor;
            set
            {
                selectedColor = value;
                Invalidate();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color TextColor
        {
            get => textColor;
            set
            {
                textColor = value;
                Invalidate();
            }
        }

        public CustomRadioButton()
        {
            InitializeComponent();
            SetupControl();
        }

        private void SetupControl()
        {
            this.Size = new Size(150, 25);
            this.BackColor = Color.Transparent;
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // 라디오 버튼 원 그리기
            int radioSize = 16;
            int radioY = (this.Height - radioSize) / 2;
            Rectangle radioRect = new Rectangle(0, radioY, radioSize, radioSize);

            // 외부 테두리 그리기 (주황색)
            using (Pen borderPen = new Pen(borderColor, 2))
            {
                g.DrawEllipse(borderPen, radioRect);
            }

            // 선택된 상태일 때 내부 원 그리기
            if (isSelected)
            {
                int innerSize = 8;
                int innerOffset = (radioSize - innerSize) / 2;
                Rectangle innerRect = new Rectangle(
                    radioRect.X + innerOffset, 
                    radioRect.Y + innerOffset, 
                    innerSize, 
                    innerSize);

                using (Brush innerBrush = new SolidBrush(selectedColor))
                {
                    g.FillEllipse(innerBrush, innerRect);
                }
            }

            // 텍스트 그리기 (라디오 버튼 오른쪽에)
            int textX = radioSize + 8;
            Rectangle textRect = new Rectangle(textX, 0, this.Width - textX, this.Height);

            using (Brush textBrush = new SolidBrush(textColor))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center
                };

                Font textFont = new Font("Noto Sans KR", 20F, FontStyle.Regular);
                g.DrawString(labelText, textFont, textBrush, textRect, sf);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            IsSelected = !IsSelected; // 토글 방식
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            this.Focus();
        }

        public void Reset()
        {
            IsSelected = false;
        }
    }
} 