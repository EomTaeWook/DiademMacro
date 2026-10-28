using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Infrastructure.Models.Updates;
using System;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Updates
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class ApplicationUpdateRestartPresenter
    {
        private readonly ApplicationUpdateInstaller _installer;
        private readonly IClassicDesktopStyleApplicationLifetime _desktop;
        private ApplicationUpdateInstallPlan _plan;
        private MainWindow _window;
        private bool _windowClosed;

        public ApplicationUpdateRestartPresenter(ApplicationUpdateInstaller installer, IClassicDesktopStyleApplicationLifetime desktop)
        {
            _installer = installer;
            _desktop = desktop;
        }

        public bool OpenSettingsAfterRestart
        {
            get
            {
                if (_desktop.Args == null)
                {
                    return false;
                }
                if (_desktop.Args.Length < 3)
                {
                    return false;
                }
                return _desktop.Args[0] == "--macro-update-cleanup";
            }
        }

        public ApplicationUpdateMessageCode StartupFailure
        {
            get
            {
                if (OpenSettingsAfterRestart == false)
                {
                    return ApplicationUpdateMessageCode.None;
                }
                if (_desktop.Args.Length != 4)
                {
                    return ApplicationUpdateMessageCode.None;
                }
                if (Enum.TryParse(_desktop.Args[3], out ApplicationUpdateMessageCode code) == false)
                {
                    return ApplicationUpdateMessageCode.None;
                }
                if (code == ApplicationUpdateMessageCode.ReplaceFailed)
                {
                    return code;
                }
                if (code == ApplicationUpdateMessageCode.LaunchFailed)
                {
                    return code;
                }
                return ApplicationUpdateMessageCode.None;
            }
        }

        public bool TryBeginUpdate()
        {
            if (_desktop.MainWindow is MainWindow window)
            {
                return window.TryBeginApplicationUpdate();
            }
            return false;
        }

        public void EndUpdate()
        {
            if (_desktop.MainWindow is MainWindow window)
            {
                window.EndApplicationUpdate();
            }
        }

        public void RequestRestart(ApplicationUpdateInstallPlan plan)
        {
            if (_desktop.MainWindow is not MainWindow window)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.CloseUnavailable);
            }
            if (window.ApplicationUpdateInProgress == false)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.CloseUnavailable);
            }
            if (window.ShutdownInProgress == true)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.CloseUnavailable);
            }
            if (_plan != null)
            {
                throw new ApplicationUpdateException(ApplicationUpdateMessageCode.PrepareFailed);
            }
            _installer.EnsureHelperReady(plan);
            _plan = plan;
            _window = window;
            _window.Closed += OnWindowClosed;
            Dispatcher.UIThread.Post(() => _window.Close());
        }

        private void OnWindowClosed(object sender, EventArgs eventArgs)
        {
            _window.Closed -= OnWindowClosed;
            _windowClosed = _window.ShutdownCompleted;
        }

        public async Task CompleteShutdownAsync()
        {
            if (_plan == null)
            {
                return;
            }
            try
            {
                if (_windowClosed == false)
                {
                    await _installer.CancelAsync(_plan).ConfigureAwait(false);
                    return;
                }
                await _installer.AuthorizeAfterShutdownAsync(_plan).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                await _installer.CancelAsync(_plan).ConfigureAwait(false);
            }
        }
    }
}
