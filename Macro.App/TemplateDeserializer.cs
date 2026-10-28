using DataContainer.Generated;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Macro
{
    public class TemplateDeserializer : ITemplateDeserializer
    {
        private readonly JsonSerializerOptions _serializerOptions;

        public TemplateDeserializer()
        {
            _serializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = false
            };
            _serializerOptions.Converters.Add(new JsonStringEnumConverter());
        }

        public IEnumerable<T> Deserialize<T>(string json) where T : TemplateBase, new()
        {
            List<T> values = JsonSerializer.Deserialize<List<T>>(json, _serializerOptions);
            if (values == null)
            {
                throw new InvalidOperationException($"Template JSON could not be deserialized. templateType:{typeof(T).FullName}");
            }
            return values;
        }
    }
}
