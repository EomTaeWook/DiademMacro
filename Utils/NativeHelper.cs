using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using Utils.Infrastructure;
using Utils.Models;

namespace Utils
{
    public class NativeHelper
    {
        [DllImport("dwmapi.dll")]
        public static extern int DwmUpdateThumbnailProperties(IntPtr thumbHandleWnd, ref DWMThumbnailProperties props);
        [DllImport("dwmapi.dll")]
        public static extern int DwmUnregisterThumbnail(IntPtr thumbHandleWnd);
        [DllImport("dwmapi.dll")]
        public static extern int DwmRegisterThumbnail(IntPtr destHandleWnd, IntPtr srcHandleWnd, out IntPtr thumbHandleWnd);
        [DllImport("dwmapi.dll")]
        public static extern int DwmQueryThumbnailSourceSize(IntPtr thumbHandleWnd, out InterSize size);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref IntRect rect);

        [DllImport("user32.dll")]
        public static extern int SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr rect, MonitorEnumDelegate callback, int data);
        public delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref IntRect rect, int data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll")]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("User32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);


        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hwnd, EnumWindowProcDelegate callback, IntPtr lParam);
        private delegate bool EnumWindowProcDelegate(IntPtr hwnd, IntPtr lParam);

        public static IList<Tuple<string, IntPtr>> GetChildHandles(IntPtr mainWindowHandle)
        {
            var childHandles = new List<Tuple<string, IntPtr>>();
            var gcHandle = GCHandle.Alloc(childHandles);
            try
            {
                EnumChildWindows(mainWindowHandle, new EnumWindowProcDelegate((hwnd, lParam) =>
                {
                    var lParamHwnd = GCHandle.FromIntPtr(lParam);
                    if (lParamHwnd.Target == null && lParamHwnd == null)
                    {
                        return false;
                    }

                    if (lParamHwnd.Target is List<Tuple<string, IntPtr>> list)
                    {
                        var sb = new StringBuilder();
                        GetWindowText(hwnd, sb, GetWindowTextLength(hwnd) + 1);
                        list.Add(Tuple.Create(sb.ToString(), hwnd));
                    }
                    return true;
                }), GCHandle.ToIntPtr(gcHandle));
            }
            finally
            {
                if (gcHandle.IsAllocated)
                {
                    gcHandle.Free();
                }

            }
            return childHandles;
        }


        public static void MouseEvent(MouseFlag mouseFlag, int x, int y)
        {
            mouse_event((int)mouseFlag, x, y, 0, IntPtr.Zero);
        }
        [DllImport("user32.dll")]
        static extern void mouse_event(int flag, int dx, int dy, int buttons, IntPtr extra);

        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int flags);

        [DllImport("user32.dll")]
        private static extern int GetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern int SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point2D lpPoint);
        public static Point GetCursorPosition()
        {
            GetCursorPos(out Point2D point);
            return point;
        }

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);
        public static Point GetSystemDPI()
        {
            Point result = new Point();
            IntPtr hDC = GetDC(IntPtr.Zero);
            result.X = GetDeviceCaps(hDC, 0x58);
            result.Y = GetDeviceCaps(hDC, 0x5A);
            ReleaseDC(IntPtr.Zero, hDC);
            return result;
        }

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, SpecialWindowHandles hWndInsertAfter, int x, int y, int width, int height, WindowPosFlags flags);
        public static bool SetWindowPos(IntPtr hWnd, IntRect rect)
        {
            return SetWindowPos(hWnd, SpecialWindowHandles.Top, rect.Left, rect.Top, rect.Width, rect.Height, WindowPosFlags.ShowWindow | WindowPosFlags.DoNotActivate | WindowPosFlags.AsynchronousWindowPosition | WindowPosFlags.DoNotCopyBits);
        }

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, WindowMessage Msg, int wParam, int lParam);

        [DllImport("user32.dll")]
        public static extern uint SendInput(uint inputCount, Input[] inputs, int structSize);

        [DllImport("shcore.dll")]
        public static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS value);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDpiAwarenessContext(PROCESS_DPI_AWARENESS value);

        [DllImport("shcore.dll")]
        public static extern uint GetProcessDpiAwareness(IntPtr handle, out PROCESS_DPI_AWARENESS awareness);

        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor(IntPtr hMonitor, DpiFlags dpiType, out uint dpiX, out uint dpiY);
        public static Point GetMonitorDPI(IntPtr hMonitor)
        {
            GetDpiForMonitor(hMonitor, DpiFlags.Effective, out uint dpiX, out uint dpiY);
            return new Point((int)dpiX, (int)dpiY);
        }

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DisplayDevice lpDisplayDevice, uint dwFlags);

        public static bool IsAnyMonitorPoweredOn()
        {
            DisplayDevice displayDevice = new DisplayDevice();
            displayDevice.cb = Marshal.SizeOf(displayDevice);
            uint monitorIndex = 0;
            while (EnumDisplayDevices(null, monitorIndex, ref displayDevice, 0))
            {
                if ((displayDevice.StateFlags & 0x00000001) != 0)
                {
                    return true;
                }
                monitorIndex++;
            }
            return false;

        }
    }
}
