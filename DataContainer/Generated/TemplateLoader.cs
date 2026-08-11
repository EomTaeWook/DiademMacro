using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DataContainer;

namespace DataContainer.Generated
{
    public interface ITemplateDeserializer
    {
        IEnumerable<T> Deserialize<T>(string json) where T : TemplateBase, new();
    }
    public partial class TemplateLoader
    {
        public static void Load(string path, ITemplateDeserializer deserializer)
        {
            TemplateContainer<ApplicationTemplate>.Load(path, "Application.json", deserializer);
            TemplateContainer<LabelTemplate>.Load(path, "Label.json", deserializer);
            TemplateContainer<MessageTemplate>.Load(path, "Message.json", deserializer);
        }
        public static void Load(Func<string, string> funcLoadJson, ITemplateDeserializer deserializer)
        {
            TemplateContainer<ApplicationTemplate>.Load("Application.json", funcLoadJson, deserializer);
            TemplateContainer<LabelTemplate>.Load("Label.json", funcLoadJson, deserializer);
            TemplateContainer<MessageTemplate>.Load("Message.json", funcLoadJson, deserializer);
        }
        public static void MakeRefTemplate()
        {
            TemplateContainer<ApplicationTemplate>.MakeRefTemplate();
            TemplateContainer<LabelTemplate>.MakeRefTemplate();
            TemplateContainer<MessageTemplate>.MakeRefTemplate();
            
            TemplateContainer<ApplicationTemplate>.Combine();
            TemplateContainer<LabelTemplate>.Combine();
            TemplateContainer<MessageTemplate>.Combine();
        }
    }
}
