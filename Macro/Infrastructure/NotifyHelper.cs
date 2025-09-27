using Macro.Models;
using System;

namespace Macro.Infrastructure
{
    internal class NotifyHelper
    {
        public static event Action<ConfigEventArgs> ConfigChanged;

        public static void InvokeNotify(NotifyEventOldType eventType,
            INotifyEventArgs args)
        {
            switch (eventType)
            {
                case NotifyEventOldType.ConfigChanged:
                    ConfigChanged?.Invoke(args as ConfigEventArgs);
                    break;
            }
        }
    }
}
