using DataContainer.Generated;
using System;
using System.IO;

namespace Macro
{
    public class TemplateDataLoader
    {
        public void Load()
        {
            TemplateLoader.Load(ReadJson, new TemplateDeserializer());
            TemplateLoader.MakeRefTemplate();
        }

        private static string ReadJson(string fileName)
        {
#if DEBUG
            string filePath = Path.Combine(AppContext.BaseDirectory, "Datas", fileName);
            if (File.Exists(filePath) == true)
            {
                return File.ReadAllText(filePath);
            }
#endif
            return PackagedResources.ReadText("Macro.Datas." + fileName);
        }
    }
}
