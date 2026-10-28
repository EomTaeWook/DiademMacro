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

        public ApplicationUpdateHelper(ApplicationUpdateFiles files)
        {
            _files = files;
        }

        public static Task<int> RunHelperAsync(string manifestPath)
        {
            ApplicationUpdateHelper helper = new ApplicationUpdateHelper(new ApplicationUpdateFiles());
            return helper.RunAsync(manifestPath);
        }

        public static Task CleanupAfterHelperAsync(string directory, int helperPid)
        {
            ApplicationUpdateHelper helper = new ApplicationUpdateHelper(new ApplicationUpdateFiles());
            return helper.CleanupAsync(directory, helperPid);
        }

        private async Task<int> RunAsync(string manifestPath)
        {
            string directory = null;
            ApplicationUpdateInstallManifest manifest = null;
            string backup = null;
            string incoming = null;
            bool replaced = false;
            bool canRestart = false;
            ApplicationUpdateMessageCode failureCode = ApplicationUpdateMessageCode.None;
            try
            {
                directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
                _files.ValidateOwnedDirectory(directory);
                if (Path.GetFileName(manifestPath) != "install.json")
                {
                    return 1;
                }
                string helperPath = Path.Combine(directory, "helper.exe");
                if (string.Equals(Environment.ProcessPath, helperPath, StringComparison.OrdinalIgnoreCase) == false)
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
                if (string.Equals(original.MainModule.FileName, manifest.InstallPath, StringComparison.OrdinalIgnoreCase) == false)
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
                string candidate = Path.Combine(directory, "verified.exe");
                _files.ValidatePlainFile(candidate);
                if (new FileInfo(candidate).Length != manifest.CandidateSize)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.DownloadIncomplete, null, Array.Empty<object>());
                }
                await _files.VerifyHashAsync(candidate, manifest.CandidateSha256, CancellationToken.None).ConfigureAwait(false);
                string unique = Guid.NewGuid().ToString("N");
                incoming = Path.Combine(Path.GetDirectoryName(manifest.InstallPath), ".macro-update-" + unique + ".new");
                backup = Path.Combine(Path.GetDirectoryName(manifest.InstallPath), ".macro-update-" + unique + ".backup");
                File.Copy(candidate, incoming, false);
                await _files.VerifyHashAsync(incoming, manifest.CandidateSha256, CancellationToken.None).ConfigureAwait(false);
                File.Replace(incoming, manifest.InstallPath, backup);
                replaced = true;
                StartInstalled(manifest, directory, ApplicationUpdateMessageCode.None);
                // Keep the original until launching the replacement has succeeded.
                _files.TryDeleteFile(backup);
                backup = null;
                return 0;
            }
            catch (Exception exception)
            {
                RecordFailure(directory, exception);
                failureCode = ApplicationUpdateMessageCode.ReplaceFailed;
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
                        RecordFailure(directory, rollbackException);
                        // Retain the original backup if the OS prevents restoration.
                        failureCode = ApplicationUpdateMessageCode.RollbackFailed;
                    }
                }
                if (canRestart == true)
                {
                    if (failureCode != ApplicationUpdateMessageCode.RollbackFailed)
                    {
                        try
                        {
                            _files.ValidatePlainFile(manifest.InstallPath);
                            await _files.VerifyHashAsync(manifest.InstallPath, manifest.InstalledSha256, CancellationToken.None).ConfigureAwait(false);
                            StartInstalled(manifest, directory, failureCode);
                        }
                        catch (Exception restartException)
                        {
                            RecordFailure(directory, restartException);
                            // No arbitrary alternate executable, elevation, or reboot scheduling.
                        }
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
                // The running helper cannot delete itself on Windows. The relaunched app removes
                // this fixed request directory after the helper PID exits; no shared temp sweep.
            }
        }

        private void RecordFailure(string directory, Exception exception)
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
                _files.WriteHelperFailure(directory, exception);
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
            if (Path.GetExtension(manifest.InstallPath).Equals(".exe", StringComparison.OrdinalIgnoreCase) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (Path.IsPathFullyQualified(manifest.WorkingDirectory) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (Directory.Exists(manifest.WorkingDirectory) == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            _files.ValidateNoLink(manifest.WorkingDirectory);
            if (manifest.InstallPath.StartsWith(_files.UpdateRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) == true)
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
            _files.ValidatePlainFile(Path.Combine(directory, "verified.exe"));
        }

        private void StartInstalled(ApplicationUpdateInstallManifest manifest, string directory, ApplicationUpdateMessageCode failureCode)
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
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.LaunchFailed, null, Array.Empty<object>());
            }
        }

    }
}
