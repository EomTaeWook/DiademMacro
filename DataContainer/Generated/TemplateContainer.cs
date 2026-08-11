
using System;
using System.Collections.Generic;
using System.IO;
namespace DataContainer.Generated
{
    public partial class TemplateContainer<T> where T : TemplateBase, new ()
    {
        public static IEnumerable<T> Values => _dataToMap.Values;
        private static readonly Dictionary<int, T> _dataToMap = new Dictionary<int, T>();
        private static readonly Dictionary<string, T> _nameKeyToMap = new Dictionary<string, T>();

        public static T Find(int id)
        {
            if (_dataToMap.ContainsKey(id) == false)
            {
                return new T();
            }
            return _dataToMap[id];
        }
        public static T Find(string name)
        {
            if (_nameKeyToMap.ContainsKey(name) == false)
            {
                return new T();
            }
            return _nameKeyToMap[name];
        }
        public static void Load(string path, string fileName, ITemplateDeserializer deserializer)
        {
            string fullPath = Path.Combine(path, fileName);
            using (var stream = new StreamReader(File.OpenRead(fullPath)))
            {
                var content = stream.ReadToEnd();
                var templateDatas = deserializer.Deserialize<T>(content);

                foreach (var template in templateDatas)
                {
                    _dataToMap.Add(template.Id, template);
                    _nameKeyToMap.Add(template.Name, template);
                }
            }
        }
        public static void Load(string fileName, Func<string, string> funcLoadJson, ITemplateDeserializer deserializer)
        {
            var json = funcLoadJson(fileName);
            var templateDatas = deserializer.Deserialize<T>(json);

            foreach (var template in templateDatas)
            {
                _dataToMap.Add(template.Id, template);
                _nameKeyToMap.Add(template.Name, template);
            }
        }
        public static void MakeRefTemplate()
        {
            foreach (var template in _dataToMap.Values)
            {
                template.MakeRefTemplate();
            }
        }
        public static void Combine()
        {
            foreach (var template in _dataToMap.Values)
            {
                template.Combine();
            }
        }
    }
}