using Utils.Infrastructure;

namespace Macro.Models
{
    public class ROICaptureCompletedEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public IntRect RoiRect { get; set; }
    }
}
