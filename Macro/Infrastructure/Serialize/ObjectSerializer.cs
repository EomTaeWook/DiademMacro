using DataContainer.Generated;
using Macro.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Macro.Infrastructure.Serialize
{
    public class ObjectSerializer
    {
        public static byte[] SerializeObject<T>(T model)
        {
            var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(r => r.CustomAttributes.Any(a => a.AttributeType == typeof(OrderAttribute)))
                                            .OrderBy(o => ((OrderAttribute)o.GetCustomAttribute(typeof(OrderAttribute))).Order);

            if (properties.Count() == 0)
                properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(r => r.CanRead && r.CanWrite)
                                            .OrderBy(o => o.Name);

            using (var ms = new MemoryStream())
            {
                var bf = new BinaryFormatter();
                bf.Serialize(ms, "\uFF1C");
                foreach (var prop in properties)
                {
                    var val = prop.GetValue(model);
                    var nullableType = Nullable.GetUnderlyingType(prop.PropertyType);
                    if (nullableType == null)
                    {
                        val = val ?? Activator.CreateInstance(prop.PropertyType);
                    }
                    else
                    {
                        val = val ?? Activator.CreateInstance(nullableType);
                    }
                    bf.Serialize(ms, val);
                }
                bf.Serialize(ms, "\uFF1E");
                return ms.ToArray();
            }
        }
        public static List<T> DeserializeObject<T>(byte[] data)
        {
            var list = new List<T>();
            using (var ms = new MemoryStream(data))
            {
                var bf = new BinaryFormatter();
                var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                            .Where(r => r.CustomAttributes.Any(a => a.AttributeType == typeof(OrderAttribute)))
                                            .OrderBy(o => ((OrderAttribute)o.GetCustomAttribute(typeof(OrderAttribute))).Order);

                if (properties.Count() == 0)
                {
                    properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Where(r => r.CanRead && r.CanWrite)
                                                .OrderBy(o => o.Name);
                }

                while (ms.Position < ms.Length)
                {
                    var startTag = bf.Deserialize(ms);
                    if (!startTag.Equals("\uFF1C"))
                    {
                        var template = TemplateContainer<MessageTemplate>.Find(1007);
                        throw new FormatException(template.GetString());
                    }
                    var obj = (T)Activator.CreateInstance(typeof(T));
                    bool isComplete = true;
                    foreach (var prop in properties)
                    {
                        var val = bf.Deserialize(ms);
                        if (val.Equals("\uFF1E"))
                        {
                            isComplete = false;
                            break;
                        }
                        prop.SetValue(obj, val);
                    }
                    if (isComplete)
                    {
                        var endTag = bf.Deserialize(ms);
                        if (!endTag.Equals("\uFF1E"))
                        {
                            var template = TemplateContainer<MessageTemplate>.Find(1007);
                            throw new FormatException(template.GetString());
                        }
                    }
                    list.Add(obj);
                }
            }
            return list;
        }
    }
}
