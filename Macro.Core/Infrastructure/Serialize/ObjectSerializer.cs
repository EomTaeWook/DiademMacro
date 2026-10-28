using DataContainer.Generated;
using Dignus.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Macro.Infrastructure.Serialize
{
    public class ObjectSerializer
    {
        public static List<T> DeserializeObject<T>(byte[] serializedBytes)
        {
            var deserializedObjects = new List<T>();
            using (var memoryStream = new MemoryStream(serializedBytes))
            {
                var bf = new BinaryFormatter()
                {
                    Binder = new TypeRedirectBinder()
                };
                var targetTypeProperties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                var maxPropertyOrder = 0;
                var propertyOrderQueue = new ArrayQueue<(int Order, PropertyInfo PropertyInfo)>();
                foreach (var prop in targetTypeProperties)
                {
                    var orderAttribute = prop.GetCustomAttribute<OrderAttribute>();
                    if (orderAttribute == null)
                    {
                        continue;
                    }
                    if (maxPropertyOrder < orderAttribute.Order)
                    {
                        maxPropertyOrder = orderAttribute.Order;
                    }
                    propertyOrderQueue.Add((orderAttribute.Order, prop));
                }

                var propertiesByOrder = new ArrayQueue<PropertyInfo>(maxPropertyOrder);

                foreach (var item in propertyOrderQueue)
                {
                    propertiesByOrder[item.Order - 1] = item.PropertyInfo;
                }

                while (memoryStream.Position < memoryStream.Length)
                {
                    var startTag = bf.Deserialize(memoryStream);
                    if (!startTag.Equals("\uFF1C"))
                    {
                        throw new FormatException("Invalid legacy save record.");
                    }
                    var deserializedObject = (T)Activator.CreateInstance(typeof(T));
                    bool isComplete = true;
                    foreach (var prop in propertiesByOrder)
                    {
                        var propertyValue = bf.Deserialize(memoryStream);
                        if (propertyValue.Equals("\uFF1E"))
                        {
                            isComplete = false;
                            break;
                        }
                        if (prop == null)
                        {
                            continue;
                        }
                        prop.SetValue(deserializedObject, propertyValue);
                    }
                    if (isComplete)
                    {
                        var endTag = bf.Deserialize(memoryStream);
                        if (!endTag.Equals("\uFF1E"))
                        {
                            throw new FormatException("Invalid legacy save record.");
                        }
                    }
                    deserializedObjects.Add(deserializedObject);
                }
            }
            return deserializedObjects;
        }
    }
}
