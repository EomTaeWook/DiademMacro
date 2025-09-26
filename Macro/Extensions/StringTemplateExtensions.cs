using DataContainer.Generated;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Utils.Models;


namespace Macro.Extensions
{
    public static class StringTemplateExtensions
    {
        public static string GetString(this LabelTemplate labelTemplate)
        {
            var config = ServiceDispatcher.GetService<Config>();
            if (config.Language == LanguageType.Kor)
            {
                return labelTemplate.Kor;
            }
            return labelTemplate.Eng;
        }
    }
}
