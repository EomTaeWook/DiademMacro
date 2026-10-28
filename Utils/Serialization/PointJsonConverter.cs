using System;
using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Utils.Serialization
{
    public class PointJsonConverter : JsonConverter<Point>
    {
        private readonly TypeConverter _pointConverter = TypeDescriptor.GetConverter(typeof(Point));

        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return (Point)_pointConverter.ConvertFromInvariantString(reader.GetString());
            }
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            JsonElement root = document.RootElement;
            return new Point(root.GetProperty("X").GetInt32(), root.GetProperty("Y").GetInt32());
        }

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(_pointConverter.ConvertToInvariantString(value));
        }
    }
}
