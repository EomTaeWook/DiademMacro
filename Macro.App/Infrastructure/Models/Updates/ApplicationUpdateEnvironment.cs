using System;
namespace Macro.Infrastructure.Models.Updates
{
    public class ApplicationUpdateEnvironment
    {
        public ApplicationUpdateEnvironment(string version, string executablePath, bool publishedSingleFile)
        {
            Version = version;
            ExecutablePath = executablePath;
            PublishedSingleFile = publishedSingleFile;
        }

        public string Version { get; }
        public string ExecutablePath { get; }
        public bool PublishedSingleFile { get; }
    }
}
