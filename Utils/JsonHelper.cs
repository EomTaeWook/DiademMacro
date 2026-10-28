using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Utils.Serialization;

namespace Utils
{
    public class JsonHelper
    {
        public static string ImageDirectoryPath { get; set; }
        private static readonly JsonSerializerOptions _compactOptions = CreateOptions(false);
        private static readonly JsonSerializerOptions _indentedOptions = CreateOptions(true);

        public static void ConfigureImageDirectory(string imageDirectoryPath)
        {
            BitmapFileJsonConverter.ImageDirectoryPath = imageDirectoryPath;
        }

        private static JsonSerializerOptions CreateOptions(bool pretty)
        {
            DefaultJsonTypeInfoResolver resolver = new();
            resolver.Modifiers.Add(IgnoreNullProperties);
            JsonSerializerOptions options = new()
            {
                IncludeFields = true,
                TypeInfoResolver = resolver,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString,
                WriteIndented = pretty
            };
            options.Converters.Add(new BitmapFileJsonConverter());
            options.Converters.Add(new PointJsonConverter());
            return options;
        }

        private static void IgnoreNullProperties(JsonTypeInfo typeInfo)
        {
            foreach (JsonPropertyInfo property in typeInfo.Properties)
            {
                Action<object, object> setter = property.Set;
                if (setter == null)
                {
                    continue;
                }
                property.Set = (instance, value) =>
                {
                    if (value == null)
                    {
                        return;
                    }
                    setter(instance, value);
                };
            }
        }
        public static T Load<T>(string path)
        {
            return DeserializeObject<T>(File.ReadAllText(path));
        }

        public static T DeserializeObject<T>(string json)
        {
            if (json != null)
            {
                if (string.IsNullOrWhiteSpace(json) == true)
                {
                    if (typeof(T).IsValueType == false)
                    {
                        return default;
                    }
                    if (Nullable.GetUnderlyingType(typeof(T)) != null)
                    {
                        return default;
                    }
                }
            }
            return JsonSerializer.Deserialize<T>(json, _compactOptions);
        }

        public static string SerializeObject(object value, bool pretty = false)
        {
            if (pretty == true)
            {
                return JsonSerializer.Serialize(value, _indentedOptions);
            }
            return JsonSerializer.Serialize(value, _compactOptions);
        }
    }
}
