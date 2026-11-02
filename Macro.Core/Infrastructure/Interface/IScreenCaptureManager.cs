using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Interface
{
    public interface IScreenCaptureManager
    {
        void SetDrawWindowHandle(IntPtr windowHandle);
        bool CaptureProcessWindow(Process process, CancellationToken cancellationToken, out Bitmap bitmap);
        Bitmap CaptureScreenRegion(MonitorInfo monitor, IntRect region, CancellationToken cancellationToken);
        IntRect AdjustRectForDPI(Utils.Infrastructure.Rectangle rectangle, MonitorInfo monitor);
        List<MonitorInfo> GetMonitorInfo();
    }
}
