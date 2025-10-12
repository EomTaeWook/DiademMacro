using Newtonsoft.Json;
using System.IO;
using Utils.Serialization;

namespace Utils
{
    public class JsonHelper
    {
        public static string ImageDirectoryPath { get; set; }
        private static JsonSerializerSettings _jsonSerializerSettings;
        static JsonHelper()
        {
            _jsonSerializerSettings = CreateSettings();
        }
        public static void ConfigureImageDirectory(string imageDirectoryPath)
        {
            BitmapFileJsonConverter.ImageDirectoryPath = imageDirectoryPath;
        }
        private static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            settings.Converters.Add(new BitmapFileJsonConverter());
            return settings;
        }

        public static T Load<T>(string path)
        {
            var json = File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }
        public static T DeserializeObject<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json, _jsonSerializerSettings);
        }
        public static string SerializeObject(object obj, bool pretty = false)
        {
            var formatting = pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
#if DEBUG
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, formatting, _jsonSerializerSettings);
#else
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, formatting, setting);
#endif
        }
    }
}
