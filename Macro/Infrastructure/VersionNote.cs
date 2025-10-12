using Utils.Models;

namespace Macro.Infrastructure
{
    internal class Current
    {
        public static readonly int Major = 3;
        public static readonly int Minor = 0;
        public static readonly int Build = 0;
    }

    public class VersionNote
    {
        public Version Version { get; set; }
        public string Description { get; set; }
        public VersionNote()
        {
        }
        public static Version CurrentVersion => new System.Lazy<Version>(() =>
                                                          {
                                                              return new Version()
                                                              {
                                                                  Major = Current.Major,
                                                                  Minor = Current.Minor,
                                                                  Build = Current.Build
                                                              };
                                                          }).Value;
    }
}
