using Macro.Infrastructure.Models.Updates;
using System;
using System.Reflection;
using Version = Utils.Models.Version;

namespace Macro.Infrastructure
{
    public class VersionNote
    {
        public Version Version { get; set; }
        public string Description { get; set; }

        public static Version CurrentVersion
        {
            get
            {
                string version = typeof(VersionNote).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
                if (ApplicationReleaseVersion.TryParse(version, out ApplicationReleaseVersion parsedVersion) == false)
                {
                    throw new InvalidOperationException("The application version is invalid.");
                }
                return new Version(parsedVersion.Major, parsedVersion.Minor, parsedVersion.Patch);
            }
        }
    }
}
