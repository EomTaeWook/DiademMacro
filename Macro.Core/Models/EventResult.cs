namespace Macro.Models
{
    internal class EventResult
    {
        public bool IsSuccess { get; private set; }
        public EventInfoModel NextEventInfoModel { get; private set; }

        public EventResult(bool success, EventInfoModel nextEventInfoModel)
        {
            IsSuccess = success;
            NextEventInfoModel = nextEventInfoModel;
        }
    }
}
