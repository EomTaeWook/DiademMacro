using System.Text.Json.Serialization;
using Utils.Serialization;

namespace Macro.Infrastructure
{
    [JsonConverter(typeof(EnumJsonConverter<MacroModeType>))]
    public enum MacroModeType
    {
        SequentialMode,
        BatchMode
    }
}
