using Macro.Infrastructure;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Controls;
using Utils.Infrastructure;

namespace Macro.Models
{
    public interface INotifyEventArgs
    { }

    public class MousePointEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public MouseTriggerInfo MouseTriggerInfo { get; set; }
    }
    public class CaptureEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public Bitmap CaptureImage { get; set; }
        public IntRect Position { get; set; }

    }
    public class ConfigEventArgs : INotifyEventArgs
    {
        public Config Config { get; set; }
    }
    public class EventTriggerOrderChangedEventArgs : INotifyEventArgs
    {
        public TreeViewItem SelectedTreeViewItem { get; set; }
    }
    public class SelctTreeViewItemChangedEventArgs : INotifyEventArgs
    {
        public TreeViewItem TreeViewItem { get; set; }
    }
    public class EventTriggerEventArgs : INotifyEventArgs
    {
        public ulong Index { get; set; }
        public EventTriggerModel TriggerModel { get; set; }
    }
    public class UpdatedTimeArgs : INotifyEventArgs
    {
        public float DeltaTime { get; set; }
    }


    public class SaveEventTriggerModelArgs : INotifyEventArgs
    {
        public EventTriggerModel CurrentEventTriggerModel { get; set; }
    }

    public class DeleteEventTriggerModelArgs : INotifyEventArgs
    {
        public EventTriggerModel CurrentEventTriggerModel { get; set; }
    }

    public class ComboProcessChangedEventArgs : INotifyEventArgs
    {
        public Process Process { get; set; }
    }

    public class TreeGridViewFocusEventArgs : INotifyEventArgs
    {
        public InitialTab Mode { get; set; }
    }
    public class ROICaptureEventArgs : INotifyEventArgs
    {
        public MonitorInfo MonitorInfo { get; set; }
        public IntRect? RoiRect { get; set; }
    }
}
