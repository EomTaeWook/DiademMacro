using System.IO;
using System.Resources;
using System.Text;

namespace Macro
{
    internal static class PackagedResources
    {
        public static string ReadText(string resourceName)
        {
            Stream stream = typeof(PackagedResources).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new MissingManifestResourceException(resourceName);
            }
            using (stream)
            using (StreamReader reader = new(stream, Encoding.UTF8, true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
