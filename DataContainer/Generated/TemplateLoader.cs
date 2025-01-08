using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using DataContainer;

namespace DataContainer.Generated
{
    public partial class TemplateLoader
    {
        public static void Load(string path)
        {
            TemplateContainer<ApplicationTemplate>.Load(path, "Application.json");
            TemplateContainer<LabelTemplate>.Load(path, "Label.json");
            TemplateContainer<MessageTemplate>.Load(path, "Message.json");
        }
        public static void Load(Func<string, string> funcLoadJson)
        {
            TemplateContainer<ApplicationTemplate>.Load("Application.json", funcLoadJson);
            TemplateContainer<LabelTemplate>.Load("Label.json", funcLoadJson);
            TemplateContainer<MessageTemplate>.Load("Message.json", funcLoadJson);
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
