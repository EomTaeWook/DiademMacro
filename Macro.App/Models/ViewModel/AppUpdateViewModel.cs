using DataContainer.Generated;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Extensions;
using Macro.Infrastructure.Models.Updates;
using Macro.Infrastructure.Updates;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Macro.Models.ViewModel
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class AppUpdateViewModel : INotifyPropertyChanged
    {
        private readonly ApplicationUpdateService _updates;
        private readonly ApplicationUpdateInstaller _installer;
        private readonly ApplicationUpdateRestartPresenter _restart;
        private readonly Config _config;
        private ApplicationUpdateCheck _check;
        private ApplicationUpdateInstallPlan _plan;
        private VerifiedApplicationUpdate _download;
        private CancellationTokenSource _requestLifetime;
        private Task _operation = Task.CompletedTask;
        private bool _busy;
        private bool _installing;
        private bool _closed;
        private bool _handedOff;
        private double _percentage;
        private int _statusTemplateId = 1021;
        private ApplicationUpdateMessageCode _statusCode;
        private Exception _failure;
        private int _failureTemplateId = 1048;

        public AppUpdateViewModel(ApplicationUpdateService updates, ApplicationUpdateInstaller installer, ApplicationUpdateRestartPresenter restart, Config config)
        {
            _updates = updates;
            _installer = installer;
            _restart = restart;
            _config = config;
            _config.PropertyChanged += Config_PropertyChanged;
            if (_restart.StartupFailure != ApplicationUpdateMessageCode.None)
            {
                _failure = new ApplicationUpdateException(_restart.StartupFailure);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Heading
        {
            get
            {
                return Label(1067);
            }
        }
        public string CurrentVersionCaption
        {
            get
            {
                return Label(1068) + ": " + _updates.CurrentVersion;
            }
        }
        public string LatestVersionCaption
        {
            get
            {
                string version = "-";
                if (_check != null)
                {
                    if (_check.HasStableRelease == true)
                    {
                        version = _check.Release.Version;
                    }
                }
                return Label(1069) + ": " + version;
            }
        }
        public string CheckButtonText
        {
            get
            {
                return Label(1070);
            }
        }
        public string InstallButtonText
        {
            get
            {
                return Label(1071);
            }
        }
        public string ReleaseButtonText
        {
            get
            {
                return Label(1072);
            }
        }
        public string CancelButtonText
        {
            get
            {
                return Label(1032);
            }
        }
        public bool IsBusy
        {
            get
            {
                return _busy;
            }
        }
        public bool IsInstalling
        {
            get
            {
                return _installing;
            }
        }
        public bool IsHandedOff
        {
            get
            {
                return _handedOff;
            }
        }
        public double Percentage
        {
            get
            {
                return _percentage;
            }
        }
        public bool CanCheck
        {
            get
            {
                if (_closed == true)
                {
                    return false;
                }
                if (_busy == true)
                {
                    return false;
                }
                return _handedOff == false;
            }
        }
        public bool CanInstall
        {
            get
            {
                if (CanCheck == false)
                {
                    return false;
                }
                if (_check == null)
                {
                    return false;
                }
                return _check.CanInstall;
            }
        }
        public bool CanCancel
        {
            get
            {
                if (_handedOff == true)
                {
                    return false;
                }
                return _busy;
            }
        }
        public string StatusText
        {
            get
            {
                if (_failure is ApplicationUpdateException updateFailure)
                {
                    return Message(GetMessageTemplateId(updateFailure.Code), updateFailure.Arguments);
                }
                if (_failure != null)
                {
                    return Message(_failureTemplateId);
                }
                if (_statusCode != ApplicationUpdateMessageCode.None)
                {
                    return Message(GetMessageTemplateId(_statusCode));
                }
                if (_statusTemplateId == 1025)
                {
                    return Message(_statusTemplateId, _check.Release.Version);
                }
                return Message(_statusTemplateId);
            }
        }
        public string InstallHint
        {
            get
            {
                if (_check == null)
                {
                    return string.Empty;
                }
                if (_check.InstallBlockedCode == ApplicationUpdateMessageCode.None)
                {
                    return string.Empty;
                }
                return Message(GetMessageTemplateId(_check.InstallBlockedCode));
            }
        }

        public Task CheckAsync()
        {
            if (CanCheck == false)
            {
                return Task.CompletedTask;
            }
            _operation = CheckCoreAsync();
            return _operation;
        }

        private async Task CheckCoreAsync()
        {
            _busy = true;
            _failure = null;
            _failureTemplateId = 1048;
            _statusCode = ApplicationUpdateMessageCode.None;
            _statusTemplateId = 1022;
            _requestLifetime = new CancellationTokenSource();
            Refresh();
            try
            {
                ApplicationUpdateCheck result = await _updates.CheckAsync(_requestLifetime.Token);
                _requestLifetime.Token.ThrowIfCancellationRequested();
                if (_closed == false)
                {
                    _check = result;
                    _statusTemplateId = 1023;
                    if (result.HasStableRelease == false)
                    {
                        _statusTemplateId = 1024;
                    }
                    if (result.HasUpdate == true)
                    {
                        _statusTemplateId = 1025;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _statusCode = ApplicationUpdateMessageCode.Cancelled;
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                _failure = exception;
                _check = null;
            }
            finally
            {
                _requestLifetime.Dispose();
                _requestLifetime = null;
                _busy = false;
                Refresh();
            }
        }

        public Task InstallAsync()
        {
            if (CanInstall == false)
            {
                return Task.CompletedTask;
            }
            _operation = InstallCoreAsync(_check.Release);
            return _operation;
        }

        private async Task InstallCoreAsync(ApplicationUpdateRelease release)
        {
            if (_restart.TryBeginUpdate() == false)
            {
                _failure = new ApplicationUpdateException(ApplicationUpdateMessageCode.WorkPending);
                Refresh();
                return;
            }
            _busy = true;
            _installing = true;
            _failure = null;
            _failureTemplateId = 1048;
            _percentage = 0;
            _statusCode = ApplicationUpdateMessageCode.Downloading;
            CancellationTokenSource lifetime = new();
            _requestLifetime = lifetime;
            Refresh();
            try
            {
                Progress<ApplicationUpdateProgress> progress = new(value =>
                {
                    if (_closed == true)
                {
                    return;
                }
                    if (lifetime.IsCancellationRequested == true)
                {
                    return;
                }
                    if (_installing == false)
                {
                    return;
                }
                    if (_statusCode != ApplicationUpdateMessageCode.Downloading)
                {
                    return;
                }
                    _percentage = value.Percentage;
                    Refresh();
                });
                _download = await _updates.DownloadAsync(release, progress, lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                _statusCode = ApplicationUpdateMessageCode.Preparing;
                Refresh();
                _plan = await _installer.PrepareAsync(_download, lifetime.Token);
                await _installer.StartHelperAsync(_plan, lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                if (_closed == true)
                {
                    throw new OperationCanceledException(lifetime.Token);
                }
                _statusCode = ApplicationUpdateMessageCode.ReadyToRestart;
                _percentage = 100;
                _restart.RequestRestart(_plan);
                _handedOff = true;
                _plan = null;
                _download = null;
            }
            catch (OperationCanceledException)
            {
                _statusCode = ApplicationUpdateMessageCode.Cancelled;
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                _failure = exception;
            }
            finally
            {
                if (_handedOff == false)
                {
                    await DiscardPreparedAsync();
                    _restart.EndUpdate();
                    _installing = false;
                    _busy = false;
                }
                _requestLifetime = null;
                lifetime.Dispose();
                Refresh();
            }
        }

        public void Cancel()
        {
            if (_handedOff == false)
            {
                _requestLifetime?.Cancel();
            }
        }

        public async Task CloseAsync()
        {
            _closed = true;
            _config.PropertyChanged -= Config_PropertyChanged;
            if (_handedOff == false)
            {
                _requestLifetime?.Cancel();
            }
            await _operation;
        }

        private async Task DiscardPreparedAsync()
        {
            ApplicationUpdateInstallPlan plan = _plan;
            VerifiedApplicationUpdate download = _download;
            _plan = null;
            _download = null;
            try
            {
                await _installer.CancelAsync(plan);
                if (download != null)
                {
                    await _updates.DiscardAsync(download);
                }
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
            }
        }

        public void OpenReleasePage()
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://github.com/EomTaeWook/DiademMacro/releases") { UseShellExecute = true });
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                _failure = exception;
                _failureTemplateId = 1047;
                Refresh();
            }
        }

        private static string Label(int templateId)
        {
            return TemplateContainer<LabelTemplate>.Find(templateId).GetString();
        }

        private static string Message(int templateId, params object[] arguments)
        {
            string value = TemplateContainer<MessageTemplate>.Find(templateId).GetString();
            if (arguments.Length == 0)
            {
                return value;
            }
            return string.Format(value, arguments);
        }

        private static int GetMessageTemplateId(ApplicationUpdateMessageCode code)
        {
            switch (code)
            {
                case ApplicationUpdateMessageCode.DeploymentRequired:
                    return 1026;
                case ApplicationUpdateMessageCode.CurrentVersionInvalid:
                    return 1027;
                case ApplicationUpdateMessageCode.QueryFailed:
                    return 1028;
                case ApplicationUpdateMessageCode.ReleaseInvalid:
                    return 1029;
                case ApplicationUpdateMessageCode.DigestMissing:
                    return 1030;
                case ApplicationUpdateMessageCode.QueryUnavailable:
                    return 1031;
                case ApplicationUpdateMessageCode.AssetUnavailable:
                    return 1032;
                case ApplicationUpdateMessageCode.DownloadIncomplete:
                    return 1033;
                case ApplicationUpdateMessageCode.DownloadUnavailable:
                    return 1034;
                case ApplicationUpdateMessageCode.DigestMismatch:
                    return 1035;
                case ApplicationUpdateMessageCode.PrepareFailed:
                    return 1036;
                case ApplicationUpdateMessageCode.ReplaceFailed:
                    return 1037;
                case ApplicationUpdateMessageCode.RollbackFailed:
                    return 1038;
                case ApplicationUpdateMessageCode.CloseUnavailable:
                    return 1039;
                case ApplicationUpdateMessageCode.DownloadRequestFailed:
                    return 1040;
                case ApplicationUpdateMessageCode.LaunchFailed:
                    return 1041;
                case ApplicationUpdateMessageCode.WorkPending:
                    return 1042;
                case ApplicationUpdateMessageCode.Downloading:
                    return 1043;
                case ApplicationUpdateMessageCode.Preparing:
                    return 1044;
                case ApplicationUpdateMessageCode.ReadyToRestart:
                    return 1045;
                case ApplicationUpdateMessageCode.Cancelled:
                    return 1046;
                default:
                    throw new ArgumentOutOfRangeException(nameof(code), code, null);
            }
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            if (eventArgs.PropertyName == nameof(Config.Language))
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            if (_closed == false)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            }
        }
    }
}
