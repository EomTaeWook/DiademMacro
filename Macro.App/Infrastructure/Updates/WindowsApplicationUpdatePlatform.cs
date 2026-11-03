using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Models.Updates;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Updates
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton, typeof(IApplicationUpdatePlatform))]
    public class WindowsApplicationUpdatePlatform : IApplicationUpdatePlatform
    {
        private readonly ApplicationUpdateFiles _files;

        public WindowsApplicationUpdatePlatform(ApplicationUpdateFiles files)
        {
            _files = files;
        }

        public string DownloadFileName => "verified.exe";

        public bool IsAssetName(string assetName, string releaseTag)
        {
            if (assetName == "Macro-" + releaseTag + "-win-x64.exe")
            {
                return true;
            }
            return assetName == "Macro.exe";
        }

        public ApplicationUpdateMessageCode GetInstallBlockedCode(ApplicationUpdateEnvironment environment)
        {
            if (OperatingSystem.IsWindows() == false)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (environment.PublishedSingleFile == false)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (Debugger.IsAttached == true)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            string executablePath = environment.ExecutablePath;
            if (string.IsNullOrWhiteSpace(executablePath) == true)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (Path.IsPathFullyQualified(executablePath) == false)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            string executableName = Path.GetFileNameWithoutExtension(executablePath);
            if (executableName.Equals("dotnet", StringComparison.OrdinalIgnoreCase) == true)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (Path.GetExtension(executablePath).Equals(".exe", StringComparison.OrdinalIgnoreCase) == false)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            string executableDirectory = Path.GetDirectoryName(executablePath);
            if (File.Exists(Path.Combine(executableDirectory, "Macro.dll")) == true)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            if (File.Exists(Path.Combine(executableDirectory, "Macro.deps.json")) == true)
            {
                return ApplicationUpdateMessageCode.DeploymentRequired;
            }
            return ApplicationUpdateMessageCode.None;
        }

        public string GetHelperPath(string directory)
        {
            return Path.Combine(directory, "helper.exe");
        }

        public void ValidateInstallPath(string executablePath)
        {
            if (Path.GetExtension(executablePath).Equals(".exe", StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
        }

        public Task PrepareHelperAsync(string executablePath, string directory, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            File.Copy(executablePath, GetHelperPath(directory), false);
            return Task.CompletedTask;
        }

        public async Task<int> InstallAndRestartAsync(ApplicationUpdateInstallManifest manifest, string directory)
        {
            string backup = null;
            string incoming = null;
            bool replaced = false;
            try
            {
                string candidate = Path.Combine(directory, DownloadFileName);
                string unique = Guid.NewGuid().ToString("N");
                incoming = Path.Combine(Path.GetDirectoryName(manifest.InstallPath), ".macro-update-" + unique + ".new");
                backup = Path.Combine(Path.GetDirectoryName(manifest.InstallPath), ".macro-update-" + unique + ".backup");
                File.Copy(candidate, incoming, false);
                await _files.VerifyHashAsync(incoming, manifest.CandidateSha256, CancellationToken.None).ConfigureAwait(false);
                File.Replace(incoming, manifest.InstallPath, backup);
                replaced = true;
                await RestartAsync(manifest, directory, ApplicationUpdateMessageCode.None).ConfigureAwait(false);
                _files.TryDeleteFile(backup);
                backup = null;
                return 0;
            }
            catch (Exception exception)
            {
                ApplicationUpdateHelper.RecordFailure(_files, directory, exception);
                ApplicationUpdateMessageCode failureCode = ApplicationUpdateMessageCode.ReplaceFailed;
                if (replaced == true)
                {
                    failureCode = ApplicationUpdateMessageCode.LaunchFailed;
                    try
                    {
                        File.Replace(backup, manifest.InstallPath, null);
                        backup = null;
                    }
                    catch (Exception rollbackException)
                    {
                        ApplicationUpdateHelper.RecordFailure(_files, directory, rollbackException);
                        failureCode = ApplicationUpdateMessageCode.RollbackFailed;
                    }
                }
                if (failureCode != ApplicationUpdateMessageCode.RollbackFailed)
                {
                    try
                    {
                        _files.ValidatePlainFile(manifest.InstallPath);
                        await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                        await RestartAsync(manifest, directory, failureCode).ConfigureAwait(false);
                    }
                    catch (Exception restartException)
                    {
                        ApplicationUpdateHelper.RecordFailure(_files, directory, restartException);
                    }
                }
                return 1;
            }
            finally
            {
                if (incoming != null)
                {
                    _files.TryDeleteFile(incoming);
                }
            }
        }

        public Task RestartAsync(ApplicationUpdateInstallManifest manifest, string directory, ApplicationUpdateMessageCode failureCode)
        {
            ProcessStartInfo start = new ProcessStartInfo(manifest.InstallPath);
            start.UseShellExecute = false;
            start.WorkingDirectory = manifest.WorkingDirectory;
            start.ArgumentList.Add("--macro-update-cleanup");
            start.ArgumentList.Add(directory);
            start.ArgumentList.Add(Environment.ProcessId.ToString(System.Globalization.CultureInfo.InvariantCulture));
            if (failureCode != ApplicationUpdateMessageCode.None)
            {
                start.ArgumentList.Add(failureCode.ToString());
            }
            using Process relaunched = Process.Start(start);
            if (relaunched == null)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.LaunchFailed);
            }
            return Task.CompletedTask;
        }
    }
}
