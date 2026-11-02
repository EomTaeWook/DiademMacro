using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Utils;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Platforms.Windows
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton, typeof(IDesktopPlatform))]
    public class WindowsDesktopPlatform : IDesktopPlatform
    {
        public bool SupportsBackgroundInput
        {
            get
            {
                return true;
            }
        }
        public bool HasInputPermission
        {
            get
            {
                return true;
            }
        }
        public bool HasCapturePermission
        {
            get
            {
                return true;
            }
        }
        public void RequestInputPermission()
        {
        }
        public void RequestCapturePermission()
        {
        }
        public IntPtr GetMainWindow(Process process)
        {
            return process.MainWindowHandle;
        }
        public string GetWindowTitle(Process process)
        {
            return process.MainWindowTitle;
        }
        public IList<Tuple<string, IntPtr>> GetChildHandles(IntPtr windowHandle)
        {
            return NativeHelper.GetChildHandles(windowHandle);
        }
        public IntPtr GetRootWindow(IntPtr windowHandle)
        {
            return NativeHelper.GetAncestor(windowHandle, NativeHelper.RootAncestor);
        }
        public bool IsMinimized(IntPtr windowHandle)
        {
            return NativeHelper.IsIconic(windowHandle);
        }
        public bool TryGetWindowBounds(IntPtr windowHandle, out IntRect bounds)
        {
            bounds = new IntRect();
            return NativeHelper.GetWindowRect(windowHandle, ref bounds) != IntPtr.Zero;
        }
        public IntPtr WindowFromPoint(Point point)
        {
            return NativeHelper.WindowFromPoint(point);
        }
        public IntPtr GetForegroundWindow()
        {
            return NativeHelper.GetForegroundWindow();
        }
        public bool ActivateWindow(IntPtr windowHandle)
        {
            return NativeHelper.SetForegroundWindow(windowHandle) != 0;
        }
        public Point GetSystemDpi()
        {
            return NativeHelper.GetSystemDPI();
        }
        public Point GetCursorPosition()
        {
            return NativeHelper.GetCursorPosition();
        }
        public bool MoveCursor(Point point)
        {
            return NativeHelper.SetCursorPos(point.X, point.Y) != 0;
        }
        public void PostMouseMessage(IntPtr windowHandle, WindowMessage message, int buttonState, int coordinates)
        {
            NativeHelper.PostMessage(windowHandle, message, buttonState, coordinates);
        }
    }
}
