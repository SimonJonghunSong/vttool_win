using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace AndroidDeviceVerificationTool.Controls
{
    public static class RichTextBoxExtensions
    {
        private const int EM_SETCHARFORMAT = 0x0444;
        private const int SCF_SELECTION = 0x0001;
        private const int CFE_LINK = 0x0020;
        private const int CFM_LINK = 0x00000020;

        [StructLayout(LayoutKind.Sequential)]
        private struct CHARFORMAT2_STRUCT
        {
            public int cbSize;
            public uint dwMask;
            public int dwEffects;
            public int yHeight;
            public int yOffset;
            public int crTextColor;
            public byte bCharSet;
            public byte bPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szFaceName;
            public short wWeight;
            public short sSpacing;
            public int crBackColor;
            public int lcid;
            public int dwReserved;
            public short sStyle;
            public short wKerning;
            public byte bUnderlineType;
            public byte bAnimation;
            public byte bRevAuthor;
            public byte bReserved1;
        }

        [DllImport("user32", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, ref CHARFORMAT2_STRUCT lParam);

        public static void SetSelectionLink(this RichTextBox rtb, bool link)
        {
            CHARFORMAT2_STRUCT cf = new CHARFORMAT2_STRUCT
            {
                cbSize = Marshal.SizeOf(typeof(CHARFORMAT2_STRUCT)),
                dwMask = CFM_LINK,
                dwEffects = link ? CFE_LINK : 0
            };

            SendMessage(rtb.Handle, EM_SETCHARFORMAT, SCF_SELECTION, ref cf);
        }

    }
}

