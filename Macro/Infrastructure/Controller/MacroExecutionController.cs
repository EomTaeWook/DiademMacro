using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Manager;
using Macro.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Controller
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    internal class MacroExecutionController
    {
        private CancellationTokenSource _cts;
        private readonly Process _fixedProcess;
        private Config _config;

        private CancellationToken _cancellationToken;

        private MacroModeControllerBase _macroModeController;
        private Action<Bitmap> _drawImageCallback;
        public MacroExecutionController(Config config)
        {
            _config = config;
        }
        public void InitializeController(Action<Bitmap> drawImageCallback)
        {
            if (_config.MacroMode == MacroModeType.SequentialMode)
            {
                _macroModeController = ServiceResolver.GetService<SequentialModeController>();
            }
            else
            {
                _macroModeController = ServiceResolver.GetService<BatchModeController>();
            }
            _macroModeController.SetDrawImageCallback(drawImageCallback);
        }
        public void Start(ArrayQueue<EventInfoModel> eventInfos)
        {
            if (_cts != null)
            {
                _cts.Dispose();
            }
            _cts = new CancellationTokenSource();
            _cancellationToken = _cts.Token;
            var _ = Task.Run(() => ProcessEventLoop(eventInfos));
        }
        public void Stop()
        {
            _cts.Cancel();
        }
        private void ProcessEventLoop(ArrayQueue<EventInfoModel> eventInfos)
        {
            ArrayQueue<Process> activeProcesses = new ArrayQueue<Process>();

            if (_fixedProcess != null)
            {
                activeProcesses.Add(_fixedProcess);
            }
            else
            {
                UniqueSet<Process> uniqueProcesses = new UniqueSet<Process>();

                foreach (var item in eventInfos)
                {
                    var processInfos = Process.GetProcessesByName(item.ProcessInfo.ProcessName);
                    uniqueProcesses.AddRange(processInfos);
                }

                foreach (var process in uniqueProcesses)
                {
                    activeProcesses.Add(process);
                }
            }

            while (_cancellationToken.IsCancellationRequested == false)
            {
                _macroModeController.Execute(
                    activeProcesses,
                    eventInfos,
                    _cancellationToken);
            }
        }
    }
}
