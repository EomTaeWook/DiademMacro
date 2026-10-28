using System;

namespace Macro.Models
{
    [Obsolete]
    public class OldEventInfoEventArgs : INotifyEventArgs
    {
        public ulong Index { get; set; }
        public EventTriggerModel TriggerModel { get; set; }
    }
}
