using Macro.Models;
using System;

namespace Macro.Infrastructure
{
    internal class NotifyHelper
    {
        public static event Action<ConfigEventArgs> ConfigChanged;
        public static event Action<CaptureCompletedEventArgs> ScreenCaptureCompleted;
        public static event Action<ROICaptureCompletedEventArgs> ROICaptureCompleted;
        public static event Action<EventInfoEventArgs> EventTriggerSaved;
        public static event Action<MouseInteractionCapturedEventArgs> MouseInteractionCaptured;
        public static void InvokeNotify(NotifyEventType eventType,
            INotifyEventArgs args)
        {
            switch (eventType)
            {
                case NotifyEventType.ConfigChanged:
                    ConfigChanged?.Invoke(args as ConfigEventArgs);
                    break;
                case NotifyEventType.ScreenCaptureCompleted:
                    ScreenCaptureCompleted?.Invoke(args as CaptureCompletedEventArgs);
                    break;
                case NotifyEventType.ROICaptureCompleted:
                    ROICaptureCompleted?.Invoke(args as ROICaptureCompletedEventArgs);
                    break;
                case NotifyEventType.EventTriggerSaved:
                    EventTriggerSaved?.Invoke(args as EventInfoEventArgs);
                    break;
                case NotifyEventType.MouseInteractionCaptured:
                    MouseInteractionCaptured?.Invoke(args as MouseInteractionCapturedEventArgs);
                    break;
            }
        }
    }
}
