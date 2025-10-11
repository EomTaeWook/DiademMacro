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


    internal class OldEventResult
    {
        public bool IsSuccess { get; private set; }
        public EventTriggerModel NextEventTrigger { get; private set; }

        public OldEventResult(bool success, EventTriggerModel nextEventTrigger)
        {
            IsSuccess = success;
            NextEventTrigger = nextEventTrigger;
        }
    }
}
