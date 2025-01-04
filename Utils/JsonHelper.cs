using System.Collections.Generic;
using System.IO;

namespace Utils
{
    public class JsonHelper
    {
        public static T Load<T>(string path)
        {
            var json = File.ReadAllText(path);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
        public static T DeserializeObject<T>(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
        public static string SerializeObject(object obj, bool pretty = false)
        {
            var setting = new Newtonsoft.Json.JsonSerializerSettings()
            {
                Converters = new List<Newtonsoft.Json.JsonConverter>
                {
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                }
            };
            var formatting = pretty ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None;
#if DEBUG
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, formatting, setting);
#else
            return Newtonsoft.Json.JsonConvert.SerializeObject(obj, formatting, setting);
#endif
        }
    }
}
