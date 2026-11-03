using Macro.Infrastructure.Models.Updates;
using System.Threading;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Updates
{
    public interface IApplicationUpdatePlatform
    {
        string DownloadFileName { get; }
        bool IsAssetName(string assetName, string releaseTag);
        ApplicationUpdateMessageCode GetInstallBlockedCode(ApplicationUpdateEnvironment environment);
        string GetHelperPath(string directory);
        void ValidateInstallPath(string executablePath);
        Task PrepareHelperAsync(string executablePath, string directory, CancellationToken cancellationToken);
        Task<int> InstallAndRestartAsync(ApplicationUpdateInstallManifest manifest, string directory);
        Task RestartAsync(ApplicationUpdateInstallManifest manifest, string directory, ApplicationUpdateMessageCode failureCode);
    }
}
