using System;
namespace Macro.Infrastructure.Models.Updates
{
    public class ApplicationUpdateCheck
    {
        internal ApplicationUpdateCheck(string currentVersion, ApplicationUpdateRelease release, bool hasUpdate, ApplicationUpdateMessageCode installBlockedCode)
        {
            CurrentVersion = currentVersion;
            Release = release;
            HasUpdate = hasUpdate;
            InstallBlockedCode = installBlockedCode;
        }

        public string CurrentVersion { get; }
        public ApplicationUpdateRelease Release { get; }
        public bool HasStableRelease
        {
            get
            {
                return Release != null;
            }
        }
        public bool HasUpdate { get; }
        public ApplicationUpdateMessageCode InstallBlockedCode { get; }
        public bool CanInstall
        {
            get
            {
                if (HasUpdate == false)
                {
                    return false;
                }
                return InstallBlockedCode == ApplicationUpdateMessageCode.None;
            }
        }
    }
}
