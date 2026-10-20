using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NaturalClicker
{
    internal static class Native
    {
        public const int WM_HOTKEY = 0x0312;
        public const uint MOD_ALT = 0x0001;
        public const uint MOD_CONTROL = 0x0002;
        public const uint MOD_SHIFT = 0x0004;
        public const uint MOD_NOREPEAT = 0x4000;

        public const uint INPUT_MOUSE = 0;
        public const uint MOUSEEVENTF_MOVE = 0x0001;
        public const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        public const uint MOUSEEVENTF_LEFTUP = 0x0004;
        public const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        public const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        public const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        public const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        public const uint MOUSEEVENTF_ABSOLUTE = 0x8000;
        public const uint MOUSEEVENTF_VIRTUALDESK = 0x4000;

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public Rectangle ToRectangle()
            {
                return Rectangle.FromLTRB(Left, Top, Right, Bottom);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern int GetDeviceCaps(IntPtr hdc, int index);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("winmm.dll")]
        public static extern uint timeBeginPeriod(uint period);

        [DllImport("winmm.dll")]
        public static extern uint timeEndPeriod(uint period);

        [DllImport("user32.dll")]
        public static extern bool MessageBeep(uint type);

        public static bool SendMouse(uint flags, int dx, int dy)
        {
            INPUT input = new INPUT();
            input.type = INPUT_MOUSE;
            input.U.mi.dx = dx;
            input.U.mi.dy = dy;
            input.U.mi.mouseData = 0;
            input.U.mi.dwFlags = flags;
            input.U.mi.time = 0;
            input.U.mi.dwExtraInfo = IntPtr.Zero;

            INPUT[] inputs = new INPUT[] { input };
            return SendInput(1, inputs, Marshal.SizeOf(typeof(INPUT))) == 1;
        }

        public static Rectangle GetVirtualScreen()
        {
            int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
            int w = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int h = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            if (w <= 0 || h <= 0)
            {
                return SystemInformation.VirtualScreen;
            }
            return new Rectangle(x, y, w, h);
        }

        public static bool MoveAbsolute(Point point)
        {
            Rectangle vs = GetVirtualScreen();
            if (vs.Width <= 1 || vs.Height <= 1)
            {
                return false;
            }

            int x = Math.Max(vs.Left, Math.Min(vs.Right - 1, point.X));
            int y = Math.Max(vs.Top, Math.Min(vs.Bottom - 1, point.Y));

            int nx = (int)Math.Round((x - vs.Left) * 65535.0 / (vs.Width - 1));
            int ny = (int)Math.Round((y - vs.Top) * 65535.0 / (vs.Height - 1));
            return SendMouse(MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_VIRTUALDESK, nx, ny);
        }

        public static Point GetCursorPosition()
        {
            POINT p;
            if (GetCursorPos(out p))
            {
                return new Point(p.X, p.Y);
            }
            return Cursor.Position;
        }

        public static Rectangle GetWindowRectangle(IntPtr hWnd)
        {
            RECT r;
            if (GetWindowRect(hWnd, out r))
            {
                return r.ToRectangle();
            }
            return Rectangle.Empty;
        }

        public static bool IsWindowOnScreen(IntPtr hWnd)
        {
            if (hWnd == IntPtr.Zero)
            {
                return false;
            }
            return IsWindowVisible(hWnd) && !IsIconic(hWnd);
        }

        public static void DragWindow(IntPtr hWnd)
        {
            ReleaseCapture();
            SendMessage(hWnd, 0x00A1, new IntPtr(2), IntPtr.Zero);
        }

        public static void BeepWarning()
        {
            MessageBeep(0x00000030);
        }
    }

    internal static class HotkeyCatalog
    {
        public static readonly string[] Names = new string[]
        {
            "F6", "F7", "F8", "F9", "F10", "F11",
            "Ctrl+F6", "Ctrl+F7", "Ctrl+F8", "Ctrl+F9", "Ctrl+F10", "Ctrl+F11"
        };

        public static bool TryParse(string text, out uint modifiers, out uint virtualKey)
        {
            modifiers = 0;
            virtualKey = 0;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            string[] parts = text.Split('+');
            string key = parts[parts.Length - 1].Trim().ToUpperInvariant();
            for (int i = 0; i < parts.Length - 1; i++)
            {
                string mod = parts[i].Trim().ToUpperInvariant();
                if (mod == "CTRL" || mod == "CONTROL")
                {
                    modifiers |= Native.MOD_CONTROL;
                }
                else if (mod == "ALT")
                {
                    modifiers |= Native.MOD_ALT;
                }
                else if (mod == "SHIFT")
                {
                    modifiers |= Native.MOD_SHIFT;
                }
                else
                {
                    return false;
                }
            }

            if (key.Length >= 2 && key[0] == 'F')
            {
                int number;
                if (int.TryParse(key.Substring(1), out number) && number >= 1 && number <= 12)
                {
                    virtualKey = (uint)(0x70 + number - 1);
                    return true;
                }
            }
            return false;
        }
    }
}
