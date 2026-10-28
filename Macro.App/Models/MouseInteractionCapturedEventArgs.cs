using Utils.Infrastructure;

namespace Macro.Models
{
    public class MouseInteractionCapturedEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public MouseEventInfoV2 MouseEventInfo { get; set; }
    }
}
