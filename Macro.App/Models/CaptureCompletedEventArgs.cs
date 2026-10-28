using System.Drawing;
using Utils.Infrastructure;

namespace Macro.Models
{
    public class CaptureCompletedEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public Bitmap CaptureImage { get; set; }
        public IntRect Position { get; set; }
    }
}
