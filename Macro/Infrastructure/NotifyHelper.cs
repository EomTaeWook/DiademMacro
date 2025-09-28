using Macro.Models;
using System;

namespace Macro.Infrastructure
{
    internal class NotifyHelper
    {
        public static event Action<ConfigEventArgs> ConfigChanged;
        public static event Action<CaptureCompletedEventArgs> ScreenCaptureCompleted;

        public static void InvokeNotify(NotifyEventType eventType,
            INotifyEventArgs args)
        {
            switch (eventType)
            {
                case NotifyEventType.ConfigChanged:
                    ConfigChanged?.Invoke(args as ConfigEventArgs);
                    break;
            }
            switch (eventType)
            {
                case NotifyEventType.ScreenCaptureCompleted:
                    ScreenCaptureCompleted?.Invoke(args as CaptureCompletedEventArgs);
                    break;
            }
        }
    }
}
