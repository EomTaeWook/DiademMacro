using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Interface
{
    public interface IDesktopPlatform
    {
        bool SupportsBackgroundInput { get; }
        bool HasInputPermission { get; }
        bool HasCapturePermission { get; }
        void RequestInputPermission();
        void RequestCapturePermission();
        IntPtr GetMainWindow(Process process);
        string GetWindowTitle(Process process);
        IList<Tuple<string, IntPtr>> GetChildHandles(IntPtr windowHandle);
        IntPtr GetRootWindow(IntPtr windowHandle);
        bool IsMinimized(IntPtr windowHandle);
        bool TryGetWindowBounds(IntPtr windowHandle, out IntRect bounds);
        IntPtr WindowFromPoint(Point point);
        IntPtr GetForegroundWindow();
        bool ActivateWindow(IntPtr windowHandle);
        Point GetSystemDpi();
        Point GetCursorPosition();
        bool MoveCursor(Point point);
        void PostMouseMessage(IntPtr windowHandle, WindowMessage message, int buttonState, int coordinates);
    }
}
