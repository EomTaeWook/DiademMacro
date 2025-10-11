using Dignus.Log;
using Macro.Models;
using System;

namespace Macro.Infrastructure
{
    public class NotifyHelperOld
    {
        public static event Action<ConfigEventArgs> ConfigChanged;
        public static event Action<MousePointEventArgs> MousePositionDataBind;
        public static event Action<CaptureCompletedEventArgs> ScreenCaptureCompleted;
        public static event Action<EventTriggerOrderChangedEventArgs> TreeItemOrderChanged;
        public static event Action<EventTriggerOrderChangedEventArgs> EventTriggerOrderChanged;
        public static event Action<OldEventInfoEventArgs> EventTriggerInserted;
        public static event Action<OldEventInfoEventArgs> EventTriggerRemoved;
        public static event Action<ComboProcessChangedEventArgs> ComboProcessChanged;

        public static event Action<SelctTreeViewItemChangedEventArgs> SelectTreeViewChanged;
        public static event Action<SaveEventTriggerModelArgs> SaveEventTriggerModel;
        public static event Action<DeleteEventTriggerModelArgs> DeleteEventTriggerModel;

        public static event Action<TreeGridViewFocusEventArgs> TreeGridViewFocus;
        public static event Action<ROICaptureCompletedEventArgs> ROICaptureCompleted;

        public static void InvokeNotify(NotifyEventOldType eventType, INotifyEventArgs args)
        {
            switch (eventType)
            {
                case NotifyEventOldType.ConfigChanged:
                    ConfigChanged?.Invoke(args as ConfigEventArgs);
                    break;
                case NotifyEventOldType.MousePointDataBind:
                    MousePositionDataBind?.Invoke(args as MousePointEventArgs);
                    break;
                case NotifyEventOldType.ScreenCaptureCompleted:
                    ScreenCaptureCompleted?.Invoke(args as CaptureCompletedEventArgs);
                    break;
                case NotifyEventOldType.TreeItemOrderChanged:
                    TreeItemOrderChanged?.Invoke(args as EventTriggerOrderChangedEventArgs);
                    break;
                case NotifyEventOldType.SelctTreeViewItemChanged:
                    SelectTreeViewChanged?.Invoke(args as SelctTreeViewItemChangedEventArgs);
                    break;
                case NotifyEventOldType.EventTriggerOrderChanged:
                    EventTriggerOrderChanged?.Invoke(args as EventTriggerOrderChangedEventArgs);
                    break;
                case NotifyEventOldType.EventTriggerInserted:
                    EventTriggerInserted?.Invoke(args as OldEventInfoEventArgs);
                    break;
                case NotifyEventOldType.EventTriggerRemoved:
                    EventTriggerRemoved?.Invoke(args as OldEventInfoEventArgs);
                    break;
                case NotifyEventOldType.Save:
                    SaveEventTriggerModel?.Invoke(args as SaveEventTriggerModelArgs);
                    break;
                case NotifyEventOldType.Delete:
                    DeleteEventTriggerModel?.Invoke(args as DeleteEventTriggerModelArgs);
                    break;
                case NotifyEventOldType.ComboProcessChanged:
                    ComboProcessChanged?.Invoke(args as ComboProcessChangedEventArgs);
                    break;
                case NotifyEventOldType.TreeGridViewFocus:
                    TreeGridViewFocus?.Invoke(args as TreeGridViewFocusEventArgs);
                    break;
                case NotifyEventOldType.ROICaptureCompleted:
                    ROICaptureCompleted?.Invoke(args as ROICaptureCompletedEventArgs);
                    break;
                case NotifyEventOldType.Max:
                default:
                    LogHelper.Debug($"what the case?");
                    break;
            }
        }
    }
}
