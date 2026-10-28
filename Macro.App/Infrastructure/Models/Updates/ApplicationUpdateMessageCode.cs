using System;
namespace Macro.Infrastructure.Models.Updates
{
    public enum ApplicationUpdateMessageCode
    {
        None,
        DeploymentRequired,
        CurrentVersionInvalid,
        QueryFailed,
        ReleaseInvalid,
        DigestMissing,
        QueryUnavailable,
        AssetUnavailable,
        DownloadIncomplete,
        DownloadUnavailable,
        DigestMismatch,
        PrepareFailed,
        ReplaceFailed,
        RollbackFailed,
        CloseUnavailable,
        DownloadRequestFailed,
        LaunchFailed,
        WorkPending,
        Downloading,
        Preparing,
        ReadyToRestart,
        Cancelled
    }
}
