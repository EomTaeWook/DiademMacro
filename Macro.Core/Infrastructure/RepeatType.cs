using System.Text.Json.Serialization;
using Utils.Serialization;

namespace Macro.Infrastructure
{
    [JsonConverter(typeof(EnumJsonConverter<RepeatType>))]
    public enum RepeatType
    {
        Count,
        RepeatOnChildEvent,
        StopOnParentImage,
        Max
    }
}
