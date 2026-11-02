using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Threading;
using Utils.Infrastructure;
using Utils.Platforms.MacOS;

namespace Macro.Infrastructure.Platforms.MacOS
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton, typeof(IScreenCaptureManager))]
    public class MacOSScreenCaptureManager : IScreenCaptureManager
    {
        private readonly IDesktopPlatform _desktop;
        public MacOSScreenCaptureManager(IDesktopPlatform desktop)
        {
            _desktop = desktop;
        }
        public void SetDrawWindowHandle(IntPtr windowHandle)
        {
            throw new PlatformNotSupportedException("A DWM thumbnail host is only used on Windows.");
        }
        public bool CaptureProcessWindow(Process process, CancellationToken cancellationToken, out Bitmap bitmap)
        {
            bitmap = null;
            if (cancellationToken.IsCancellationRequested == true)
            {
                return false;
            }
            EnsureCapturePermission();
            IntPtr window = _desktop.GetMainWindow(process);
            if (window == IntPtr.Zero)
            {
                return false;
            }
            byte[] bytes = MacOSNative.CaptureAsync(true, (uint)window.ToInt64(), cancellationToken).GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            using MemoryStream stream = new(bytes, false);
            bitmap = new Bitmap(stream);
            return true;
        }
        public Bitmap CaptureScreenRegion(MonitorInfo monitor, IntRect region, CancellationToken cancellationToken)
        {
            EnsureCapturePermission();
            uint display = uint.Parse(monitor.DeviceName, CultureInfo.InvariantCulture);
            byte[] bytes = MacOSNative.CaptureAsync(false, display, cancellationToken).GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            using MemoryStream stream = new(bytes, false);
            using Bitmap screen = new(stream);
            return screen.Clone(new System.Drawing.Rectangle(region.Left, region.Top, region.Width, region.Height));
        }
        public IntRect AdjustRectForDPI(Utils.Infrastructure.Rectangle rectangle, MonitorInfo monitor)
        {
            return new IntRect { Left = rectangle.Left, Top = rectangle.Top, Right = rectangle.Right, Bottom = rectangle.Bottom };
        }
        public List<MonitorInfo> GetMonitorInfo()
        {
            return MacOSNative.GetMonitors();
        }
        private void EnsureCapturePermission()
        {
            if (_desktop.HasCapturePermission == false)
            {
                throw new UnauthorizedAccessException("Allow screen recording for Diadem Macro from Settings before capturing.");
            }
        }
    }
}
