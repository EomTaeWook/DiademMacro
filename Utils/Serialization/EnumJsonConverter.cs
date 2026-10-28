using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utils.Serialization
{
    public class EnumJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return Enum.Parse<TEnum>(reader.GetString(), true);
            }
            return (TEnum)Enum.ToObject(typeof(TEnum), reader.GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(Convert.ToInt32(value, CultureInfo.InvariantCulture));
        }
    }
}
