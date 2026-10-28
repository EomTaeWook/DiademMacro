using System.Text.Json.Serialization;
using Utils.Serialization;

namespace Macro.Infrastructure
{
    [JsonConverter(typeof(EnumJsonConverter<EventType>))]
    public enum EventType
    {
        Mouse,
        Keyboard,
        Image,
        RelativeToImage,
        Max
    }
}
