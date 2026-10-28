using DataContainer.Generated;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Utils.Models;


namespace Macro.Extensions
{
    public static class MessageTemplateExtensions
    {
        public static string GetString(this MessageTemplate messageTemplate)
        {
            var config = ServiceResolver.GetService<Config>();
            if (config.Language == LanguageType.Kor)
            {
                return messageTemplate.Kor;
            }
            return messageTemplate.Eng;
        }
        public static string GetString(this MessageTemplate messageTemplate, params string[] args)
        {
            var config = ServiceResolver.GetService<Config>();
            if (config.Language == LanguageType.Kor)
            {
                return string.Format(messageTemplate.Kor, args);
            }
            return string.Format(messageTemplate.Eng, args);
        }
    }
}
