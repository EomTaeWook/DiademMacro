namespace Macro.Models
{
    public class EventInfoEventArgs : INotifyEventArgs
    {
        public ulong Index { get; set; }
        public EventInfoModel EventInfoModel { get; set; }
    }
}
