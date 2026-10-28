using System;
using Utils.Infrastructure;

namespace Macro.Models
{
    [Obsolete]
    public class MousePointEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public MouseEventInfo MouseEventInfo { get; set; }
    }
}
