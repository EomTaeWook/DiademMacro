﻿using System;

namespace Macro.Infrastructure
{
    public class NotifyHelper
    {
        public static event Action<ConfigEventArgs> ConfigChanged;
        public static event Action<MousePointEventArgs> MousePositionDataBind;
        public static event Action<CaptureEventArgs> ScreenCaptureDataBind;
        public static event Action<EventTriggerOrderChangedEventArgs> TreeItemOrderChanged;
        public static event Action<EventTriggerOrderChangedEventArgs> EventTriggerOrderChanged;
        public static event Action<EventTriggerEventArgs> EventTriggerInserted;
        public static event Action<EventTriggerEventArgs> EventTriggerRemoved;

        public static event Action<SelctTreeViewItemChangedEventArgs> SelectTreeViewChanged;
        

        public static void InvokeNotify(EventType eventType, INotifyEventArgs args)
        {
            switch(eventType)
            {
                case EventType.ConfigChanged:
                    ConfigChanged? .Invoke(args as ConfigEventArgs);
                    break;
                case EventType.MousePointDataBind:
                    MousePositionDataBind? .Invoke(args as MousePointEventArgs);
                    break;
                case EventType.ScreenCapture:
                    ScreenCaptureDataBind? .Invoke(args as CaptureEventArgs);
                    break;
                case EventType.TreeItemOrderChanged:
                    TreeItemOrderChanged? .Invoke(args as EventTriggerOrderChangedEventArgs);
                    break;
                case EventType.SelctTreeViewItemChanged:
                    SelectTreeViewChanged?.Invoke(args as SelctTreeViewItemChangedEventArgs);
                    break;
                case EventType.EventTriggerOrderChanged:
                    EventTriggerOrderChanged?.Invoke(args as EventTriggerOrderChangedEventArgs);
                    break;
                case EventType.EventTriggerInserted:
                    EventTriggerInserted?.Invoke(args as EventTriggerEventArgs);
                    break;
                case EventType.EventTriggerRemoved:
                    EventTriggerRemoved?.Invoke(args as EventTriggerEventArgs);
                    break;
            }
        }
    }
}
