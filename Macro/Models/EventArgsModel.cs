using System;
using System.Drawing;
using Utils.Infrastructure;

namespace Macro.Models
{
    public interface INotifyEventArgs
    { }
    [Obsolete]
    public class OldEventInfoEventArgs : INotifyEventArgs
    {
        public ulong Index { get; set; }
        public EventTriggerModel TriggerModel { get; set; }
    }
    [Obsolete]
    public class MousePointEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public MouseEventInfo MouseEventInfo { get; set; }
    }
    public class MouseInteractionCapturedEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public MouseEventInfoV2 MouseEventInfo { get; set; }
    }
    public class CaptureCompletedEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public Bitmap CaptureImage { get; set; }
        public IntRect Position { get; set; }
    }
    public class ConfigEventArgs : INotifyEventArgs
    {
        public Config Config { get; set; }
    }
    public class EventInfoEventArgs : INotifyEventArgs
    {
        public ulong Index { get; set; }
        public EventInfoModel EventInfoModel { get; set; }
    }

    public class ROICaptureCompletedEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public IntRect RoiRect { get; set; }
    }
}
