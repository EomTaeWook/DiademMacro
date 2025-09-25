using Dignus.Log;
using Macro.Models;
using System;

namespace Macro.Infrastructure
{
    public class NotifyHelper
    {
        public static event Action<UpdatedTimeArgs> UpdatedTime;

        public static event Action<ConfigEventArgs> ConfigChanged;
        public static event Action<MousePointEventArgs> MousePositionDataBind;
        public static event Action<CaptureEventArgs> ScreenCaptureCompleted;
        public static event Action<EventTriggerOrderChangedEventArgs> TreeItemOrderChanged;
        public static event Action<EventTriggerOrderChangedEventArgs> EventTriggerOrderChanged;
        public static event Action<EventTriggerEventArgs> EventTriggerInserted;
        public static event Action<EventTriggerEventArgs> EventTriggerRemoved;
        public static event Action<ComboProcessChangedEventArgs> ComboProcessChanged;

        public static event Action<SelctTreeViewItemChangedEventArgs> SelectTreeViewChanged;
        public static event Action<SaveEventTriggerModelArgs> SaveEventTriggerModel;
        public static event Action<DeleteEventTriggerModelArgs> DeleteEventTriggerModel;

        public static event Action<TreeGridViewFocusEventArgs> TreeGridViewFocus;
        public static event Action<ROICaptureEventArgs> ROICaptureCompleted;

        public static void InvokeNotify(NotifyEventType eventType, INotifyEventArgs args)
        {
            switch (eventType)
            {
                case NotifyEventType.ConfigChanged:
                    ConfigChanged?.Invoke(args as ConfigEventArgs);
                    break;
                case NotifyEventType.MousePointDataBind:
                    MousePositionDataBind?.Invoke(args as MousePointEventArgs);
                    break;
                case NotifyEventType.ScreenCaptureCompleted:
                    ScreenCaptureCompleted?.Invoke(args as CaptureEventArgs);
                    break;
                case NotifyEventType.TreeItemOrderChanged:
                    TreeItemOrderChanged?.Invoke(args as EventTriggerOrderChangedEventArgs);
                    break;
                case NotifyEventType.SelctTreeViewItemChanged:
                    SelectTreeViewChanged?.Invoke(args as SelctTreeViewItemChangedEventArgs);
                    break;
                case NotifyEventType.EventTriggerOrderChanged:
                    EventTriggerOrderChanged?.Invoke(args as EventTriggerOrderChangedEventArgs);
                    break;
                case NotifyEventType.EventTriggerInserted:
                    EventTriggerInserted?.Invoke(args as EventTriggerEventArgs);
                    break;
                case NotifyEventType.EventTriggerRemoved:
                    EventTriggerRemoved?.Invoke(args as EventTriggerEventArgs);
                    break;
                case NotifyEventType.Save:
                    SaveEventTriggerModel?.Invoke(args as SaveEventTriggerModelArgs);
                    break;
                case NotifyEventType.Delete:
                    DeleteEventTriggerModel?.Invoke(args as DeleteEventTriggerModelArgs);
                    break;
                case NotifyEventType.ComboProcessChanged:
                    ComboProcessChanged?.Invoke(args as ComboProcessChangedEventArgs);
                    break;
                case NotifyEventType.TreeGridViewFocus:
                    TreeGridViewFocus?.Invoke(args as TreeGridViewFocusEventArgs);
                    break;
                case NotifyEventType.UpdatedTime:
                    UpdatedTime?.Invoke(args as UpdatedTimeArgs);
                    break;
                case NotifyEventType.ROICaptureCompleted:
                    ROICaptureCompleted?.Invoke(args as ROICaptureEventArgs);
                    break;
                case NotifyEventType.Max:
                default:
                    LogHelper.Debug($"what the case?");
                    break;
            }
        }
    }
}
