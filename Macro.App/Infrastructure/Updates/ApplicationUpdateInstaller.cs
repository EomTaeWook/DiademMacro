using Macro.Infrastructure.Models.Updates;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dignus.DependencyInjection.Attributes;

namespace Macro.Infrastructure.Updates
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class ApplicationUpdateInstaller
    {
        private readonly ApplicationUpdateService _updates;
        private readonly ApplicationUpdateFiles _files;

        public ApplicationUpdateInstaller(ApplicationUpdateService updates, ApplicationUpdateFiles files)
        {
            _updates = updates;
            _files = files;
        }

        public Task<ApplicationUpdateInstallPlan> PrepareAsync(VerifiedApplicationUpdate update, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(update);
            return Task.Run(() => PrepareCoreAsync(update, cancellationToken), cancellationToken);
        }

        private async Task<ApplicationUpdateInstallPlan> PrepareCoreAsync(VerifiedApplicationUpdate update, CancellationToken cancellationToken)
        {
            string directory = null;
            try
            {
                _updates.ValidateInstallEnvironment();
                _files.ValidateOwnedDirectory(update.Directory);
                _files.ValidatePlainFile(update.FilePath);
                await _files.VerifyHashAsync(update.FilePath, update.Release.Sha256, cancellationToken).ConfigureAwait(false);
                if (new FileInfo(update.FilePath).Length != update.Release.Size)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.DownloadIncomplete, null, Array.Empty<object>());
                }
                string installed = Path.GetFullPath(_updates.Environment.ExecutablePath);
                _files.ValidatePlainFile(installed);
                using Process original = Process.GetCurrentProcess();
                string installedHash = await _files.GetHashAsync(installed, cancellationToken).ConfigureAwait(false);
                directory = _files.CreateOwnedDirectory();
                string candidate = Path.Combine(directory, "verified.exe");
                File.Copy(update.FilePath, candidate, false);
                File.Copy(installed, Path.Combine(directory, "helper.exe"), false);
                cancellationToken.ThrowIfCancellationRequested();
                ApplicationUpdateInstallManifest manifest = new ApplicationUpdateInstallManifest();
                manifest.OriginalPid = original.Id;
                manifest.OriginalStartTicks = original.StartTime.ToUniversalTime().Ticks;
                manifest.InstallPath = installed;
                manifest.InstalledSha256 = installedHash;
                manifest.CandidateSha256 = update.Release.Sha256;
                manifest.CandidateSize = update.Release.Size;
                manifest.Nonce = Guid.NewGuid().ToString("N");
                manifest.WorkingDirectory = Path.GetFullPath(System.Environment.CurrentDirectory);
                await File.WriteAllTextAsync(Path.Combine(directory, "install.json"), JsonSerializer.Serialize(manifest), cancellationToken).ConfigureAwait(false);
                return new ApplicationUpdateInstallPlan(directory, manifest.Nonce, update);
            }
            catch (OperationCanceledException)
            {
                if (directory != null)
                {
                    _files.DeleteOwnedDirectory(directory);
                }
                throw;
            }
            catch (Exception exception)
            {
                if (directory != null)
                {
                    _files.DeleteOwnedDirectory(directory);
                }
                if (exception is ApplicationUpdateException)
                {
                    throw;
                }
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, exception, Array.Empty<object>()) { ReleasePage = update.Release.ReleasePage };
            }
        }

        public Task StartHelperAsync(ApplicationUpdateInstallPlan plan, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => StartHelperCoreAsync(plan, cancellationToken), cancellationToken);
        }

        private async Task StartHelperCoreAsync(ApplicationUpdateInstallPlan plan, CancellationToken cancellationToken)
        {
            try
            {
                _files.ValidateOwnedDirectory(plan.Directory);
                cancellationToken.ThrowIfCancellationRequested();
                ProcessStartInfo start = new ProcessStartInfo(Path.Combine(plan.Directory, "helper.exe"));
                start.UseShellExecute = false;
                start.CreateNoWindow = true;
                start.WorkingDirectory = plan.Directory;
                start.ArgumentList.Add("--macro-update-helper");
                start.ArgumentList.Add(Path.Combine(plan.Directory, "install.json"));
                plan.Helper = Process.Start(start);
                if (plan.Helper == null)
                {
                    throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
                }
                using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(15));
                while (File.Exists(Path.Combine(plan.Directory, "ready")) == false)
                {
                    if (plan.Helper.HasExited == true)
                    {
                        throw CreateHelperFailure(plan);
                    }
                    await Task.Delay(100, timeout.Token).ConfigureAwait(false);
                }
                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested == true)
                {
                    throw;
                }
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            catch (ApplicationUpdateException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, exception, Array.Empty<object>());
            }
        }

        public void EnsureHelperReady(ApplicationUpdateInstallPlan plan)
        {
            ArgumentNullException.ThrowIfNull(plan);
            _files.ValidateOwnedDirectory(plan.Directory);
            if (plan.Helper == null)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            if (plan.Helper.HasExited == true)
            {
                throw CreateHelperFailure(plan);
            }
        }

        private ApplicationUpdateException CreateHelperFailure(ApplicationUpdateInstallPlan plan)
        {
            string helperFailure = _files.ReadHelperFailure(plan.Directory);
            if (string.IsNullOrEmpty(helperFailure) == true)
            {
                return new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, null, Array.Empty<object>());
            }
            return new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed, new InvalidOperationException(helperFailure), Array.Empty<object>());
        }

        public Task AuthorizeAfterShutdownAsync(ApplicationUpdateInstallPlan plan)
        {
            return Task.Run(() => AuthorizeAfterShutdown(plan));
        }

        private void AuthorizeAfterShutdown(ApplicationUpdateInstallPlan plan)
        {
            EnsureHelperReady(plan);
            File.WriteAllText(Path.Combine(plan.Directory, "authorized"), plan.Nonce);
            // The helper owns its separately verified copy. Keep the UI download on declined
            // shutdown, but remove it after the accepted handoff so it is not orphaned.
            try
            {
                _files.DeleteOwnedDownload(plan.Download);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (ApplicationUpdateException)
            {
                // A refused temporary cleanup does not revoke the accepted installation.
            }
            plan.Helper?.Dispose();
        }

        public Task CancelAsync(ApplicationUpdateInstallPlan plan)
        {
            return Task.Run(() => CancelCoreAsync(plan));
        }

        private async Task CancelCoreAsync(ApplicationUpdateInstallPlan plan)
        {
            if (plan == null)
            {
                return;
            }
            try
            {
                _files.ValidateOwnedDirectory(plan.Directory);
                File.WriteAllText(Path.Combine(plan.Directory, "cancel"), string.Empty);
                if (plan.Helper != null)
                {
                    using CancellationTokenSource timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    await plan.Helper.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                }
                _files.DeleteOwnedDirectory(plan.Directory);
                _files.DeleteOwnedDownload(plan.Download);
            }
            catch (IOException)
            {
                // Only this request's temporary files may be left; never touch the installation.
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (OperationCanceledException)
            {
                // Do not kill a helper or the original application to finish cleanup.
            }
            finally
            {
                plan.Helper?.Dispose();
            }
        }

    }
}
