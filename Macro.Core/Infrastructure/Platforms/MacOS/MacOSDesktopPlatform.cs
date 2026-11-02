using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Utils.Infrastructure;
using Utils.Platforms.MacOS;

namespace Macro.Infrastructure.Platforms.MacOS
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton, typeof(IDesktopPlatform))]
    public class MacOSDesktopPlatform : IDesktopPlatform
    {
        public bool SupportsBackgroundInput
        {
            get
            {
                return false;
            }
        }
        public bool HasInputPermission
        {
            get
            {
                return MacOSNative.InputPermission(0) != 0;
            }
        }
        public bool HasCapturePermission
        {
            get
            {
                return MacOSNative.CapturePermission(0) != 0;
            }
        }
        public void RequestInputPermission()
        {
            MacOSNative.InputPermission(1);
        }
        public void RequestCapturePermission()
        {
            MacOSNative.CapturePermission(1);
        }
        public IntPtr GetMainWindow(Process process)
        {
            return new IntPtr(MacOSNative.MainWindow(process.Id));
        }
        public string GetWindowTitle(Process process)
        {
            return MacOSNative.GetWindowTitle(MacOSNative.MainWindow(process.Id));
        }
        public IList<Tuple<string, IntPtr>> GetChildHandles(IntPtr windowHandle)
        {
            return new List<Tuple<string, IntPtr>>();
        }
        public IntPtr GetRootWindow(IntPtr windowHandle)
        {
            if (MacOSNative.TryGetWindowBounds(windowHandle, out IntRect bounds) == false)
            {
                return IntPtr.Zero;
            }
            return windowHandle;
        }
        public bool IsMinimized(IntPtr windowHandle)
        {
            return MacOSNative.WindowVisible((uint)windowHandle.ToInt64()) == 0;
        }
        public bool TryGetWindowBounds(IntPtr windowHandle, out IntRect bounds)
        {
            return MacOSNative.TryGetWindowBounds(windowHandle, out bounds);
        }
        public IntPtr WindowFromPoint(Point point)
        {
            return new IntPtr(MacOSNative.WindowAtPoint(point.X, point.Y));
        }
        public IntPtr GetForegroundWindow()
        {
            return new IntPtr(MacOSNative.ForegroundWindow());
        }
        public bool ActivateWindow(IntPtr windowHandle)
        {
            return MacOSNative.ActivateWindow((uint)windowHandle.ToInt64()) != 0;
        }
        public Point GetSystemDpi()
        {
            return new Point(96, 96);
        }
        public Point GetCursorPosition()
        {
            return MacOSNative.GetCursorPosition();
        }
        public bool MoveCursor(Point point)
        {
            return MacOSNative.Mouse(5, 0, point.X, point.Y, 0) != 0;
        }
        public void PostMouseMessage(IntPtr windowHandle, WindowMessage message, int buttonState, int coordinates)
        {
            throw new PlatformNotSupportedException("macOS does not support Win32 background mouse messages. Use physical input.");
        }
    }
}
