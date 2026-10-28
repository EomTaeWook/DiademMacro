using System.Text.Json.Serialization;
using Utils.Serialization;

namespace Macro.Infrastructure
{
    [JsonConverter(typeof(EnumJsonConverter<MouseEventType>))]
    public enum MouseEventType
    {
        None,
        LeftClick,
        RightClick,
        Drag,
        Wheel,
        Max
    }
}
