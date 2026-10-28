using System;
namespace Macro.Infrastructure.Models.Updates
{
    public class VerifiedApplicationUpdate
    {
        internal VerifiedApplicationUpdate(ApplicationUpdateRelease release, string directory, string path)
        {
            Release = release;
            Directory = directory;
            FilePath = path;
        }

        public ApplicationUpdateRelease Release { get; }
        internal string Directory { get; }
        internal string FilePath { get; }
    }
}
