using Macro.Infrastructure.Models.Updates;
using Dignus.Log;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System;
using System.Diagnostics;
using System.Text.Json;

namespace Macro.Infrastructure.Updates
{
    public class ApplicationUpdateHelper
    {
        private readonly ApplicationUpdateFiles _files;
        private readonly IApplicationUpdatePlatform _platform;
        private static readonly StringComparison _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        public ApplicationUpdateHelper(ApplicationUpdateFiles files, IApplicationUpdatePlatform platform)
        {
            _files = files;
            _platform = platform;
        }

        public static Task<int> RunHelperAsync(string manifestPath)
        {
            ApplicationUpdateFiles files = new ApplicationUpdateFiles();
#if WINDOWS
            IApplicationUpdatePlatform platform = new WindowsApplicationUpdatePlatform(files);
#else
            IApplicationUpdatePlatform platform = new MacOSApplicationUpdatePlatform(files);
#endif
            ApplicationUpdateHelper helper = new ApplicationUpdateHelper(files, platform);
            return helper.RunAsync(manifestPath);
        }

        public static Task CleanupAfterHelperAsync(string directory, int helperPid)
        {
            ApplicationUpdateFiles files = new ApplicationUpdateFiles();
#if WINDOWS
            IApplicationUpdatePlatform platform = new WindowsApplicationUpdatePlatform(files);
#else
            IApplicationUpdatePlatform platform = new MacOSApplicationUpdatePlatform(files);
#endif
            ApplicationUpdateHelper helper = new ApplicationUpdateHelper(files, platform);
            return helper.CleanupAsync(directory, helperPid);
        }

        private async Task<int> RunAsync(string manifestPath)
        {
            string directory = null;
            ApplicationUpdateInstallManifest manifest = null;
            bool canRestart = false;
            try
            {
                directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
                _files.ValidateOwnedDirectory(directory);
                if (Path.GetFileName(manifestPath) != "install.json")
                {
                    return 1;
                }
                string helperPath = _platform.GetHelperPath(directory);
                if (string.Equals(Environment.ProcessPath, helperPath, _pathComparison) == false)
                {
                    return 1;
                }
                _files.ValidatePlainFile(manifestPath);
                if (new FileInfo(manifestPath).Length > 16384)
                {
                    return 1;
                }
                manifest = JsonSerializer.Deserialize<ApplicationUpdateInstallManifest>(await File.ReadAllTextAsync(manifestPath).ConfigureAwait(false));
                ValidateManifest(manifest, directory);
                await _files.VerifyHashAsync(helperPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                using Process original = Process.GetProcessById(manifest.OriginalPid);
                if (original.StartTime.ToUniversalTime().Ticks != manifest.OriginalStartTicks)
                {
                    return 1;
                }
                if (original.MainModule == null)
                {
                    return 1;
                }
                if (string.Equals(original.MainModule.FileName, manifest.InstallPath, _pathComparison) == false)
                {
                    return 1;
                }
                _files.ValidatePlainFile(manifest.InstallPath);
                await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                File.WriteAllText(Path.Combine(directory, "ready"), string.Empty);
                DateTime deadlineUtc = DateTime.UtcNow.AddMinutes(10);
                while (original.HasExited == false)
                {
                    if (File.Exists(Path.Combine(directory, "cancel")) == true)
                    {
                        return 0;
                    }
                    if (DateTime.UtcNow >= deadlineUtc)
                    {
                        return 1;
                    }
                    await Task.Delay(100).ConfigureAwait(false);
                }
                string authorization = Path.Combine(directory, "authorized");
                if (File.Exists(authorization) == false)
                {
                    return 0;
                }
                if (await File.ReadAllTextAsync(authorization).ConfigureAwait(false) != manifest.Nonce)
                {
                    return 1;
                }
                if (File.Exists(Path.Combine(directory, "cancel")) == true)
                {
                    return 0;
                }
                canRestart = true;
                // Fixed target must still be the original executable, not a replaced path or link.
                _files.ValidatePlainFile(manifest.InstallPath);
                await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                string candidate = Path.Combine(directory, _platform.DownloadFileName);
                _files.ValidatePlainFile(candidate);
                if (new FileInfo(candidate).Length != manifest.CandidateSize)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.DownloadIncomplete, null, Array.Empty<object>());
                }
                await _files.VerifyHashAsync(candidate, manifest.CandidateSha256, CancellationToken.None).ConfigureAwait(false);
                return await _platform.InstallAndRestartAsync(manifest, directory).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                RecordFailure(_files, directory, exception);
                if (canRestart == true)
                {
                    try
                    {
                        _files.ValidatePlainFile(manifest.InstallPath);
                        await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                        await _platform.RestartAsync(manifest, directory, ApplicationUpdateMessageCode.ReplaceFailed).ConfigureAwait(false);
                    }
                    catch (Exception restartException)
                    {
                        RecordFailure(_files, directory, restartException);
                    }
                }
                return 1;
            }
        }

        internal static void RecordFailure(ApplicationUpdateFiles files, string directory, Exception exception)
        {
            try
            {
                Console.Error.WriteLine(exception);
            }
            catch (IOException)
            {
            }
            if (directory == null)
            {
                return;
            }
            try
            {
                files.WriteHelperFailure(directory, exception);
            }
            catch (Exception diagnosticException)
            {
                try
                {
                    Console.Error.WriteLine(diagnosticException);
                }
                catch (IOException)
                {
                }
            }
        }

        private Task CleanupAsync(string directory, int helperPid)
        {
            return Task.Run(async () =>
            {
                try
                {
                    _files.ValidateOwnedDirectory(directory);
                    if (helperPid == Environment.ProcessId)
                    {
                        return;
                    }
                    try
                    {
                        using Process helper = Process.GetProcessById(helperPid);
                        using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                        await helper.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                    }
                    catch (ArgumentException)
                    {
                        // The helper has already exited.
                    }
                    string helperFailure = _files.ReadHelperFailure(directory);
                    if (string.IsNullOrEmpty(helperFailure) == false)
                    {
                        LogHelper.Error(helperFailure);
                    }
                    _files.DeleteOwnedDirectory(directory);
                }
                catch (Exception)
                {
                    // Best-effort removal of this specific updater directory, without logging paths.
                }
            });
        }

        private void ValidateManifest(ApplicationUpdateInstallManifest manifest, string directory)
        {
            if (manifest == null)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (manifest.OriginalPid <= 0)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (manifest.OriginalPid == Environment.ProcessId)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (Path.IsPathFullyQualified(manifest.InstallPath) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            _platform.ValidateInstallPath(manifest.InstallPath);
            if (Path.IsPathFullyQualified(manifest.WorkingDirectory) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (Directory.Exists(manifest.WorkingDirectory) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            _files.ValidateNoLink(manifest.WorkingDirectory);
            if (manifest.InstallPath.StartsWith(_files.UpdateRoot + Path.DirectorySeparatorChar, _pathComparison) == true)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (Guid.TryParseExact(manifest.Nonce, "N", out Guid _) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (ApplicationUpdateService.IsSha256(manifest.InstalledSha256) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (ApplicationUpdateService.IsSha256(manifest.CandidateSha256) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (manifest.CandidateSize <= 0)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            _files.ValidatePlainFile(Path.Combine(directory, _platform.DownloadFileName));
        }

    }
}
