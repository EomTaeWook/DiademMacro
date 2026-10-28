using System.Text.Json.Serialization;
using Utils.Serialization;

namespace Utils.Models
{
    [JsonConverter(typeof(EnumJsonConverter<LanguageType>))]
    public enum LanguageType
    {
        Kor,
        Eng,

        Max,
    }
}
