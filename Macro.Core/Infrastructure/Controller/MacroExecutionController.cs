using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Macro.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Controller
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class MacroExecutionController
    {
        private CancellationTokenSource _cancellationTokenSource;
        private readonly Config _config;
        private readonly SequentialModeController _sequentialModeController;
        private readonly BatchModeController _batchModeController;
        private MacroModeControllerBase _macroModeController;
        private Task _executionTask = Task.CompletedTask;

        public Task Completion
        {
            get
            {
                return _executionTask;
            }
        }

        public MacroExecutionController(Config config, SequentialModeController sequentialModeController, BatchModeController batchModeController)
        {
            _config = config;
            _sequentialModeController = sequentialModeController;
            _batchModeController = batchModeController;
        }

        public void InitializeController(Action<Bitmap> drawImageCallback, Action<EventInfoModel, string> statusCallback)
        {
            if (_config.MacroMode == MacroModeType.SequentialMode)
            {
                _macroModeController = _sequentialModeController;
            }
            else
            {
                _macroModeController = _batchModeController;
            }
            _macroModeController.SetDrawImageCallback(drawImageCallback);
            _macroModeController.SetStatusCallback(statusCallback);
        }

        public void Start(ArrayQueue<EventInfoModel> eventInfos, Process fixedProcess)
        {
            if (_executionTask.IsCompleted == false)
            {
                throw new InvalidOperationException("Macro execution is already running.");
            }
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationTokenSource.Token;
            _executionTask = Task.Run(() => ProcessEventLoop(eventInfos, fixedProcess, cancellationToken));
        }

        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
        }

        public async Task StopAsync()
        {
            CancellationTokenSource cancellationTokenSource = _cancellationTokenSource;
            Task executionTask = _executionTask;
            cancellationTokenSource?.Cancel();
            try
            {
                await executionTask;
            }
            finally
            {
                if (_cancellationTokenSource == cancellationTokenSource)
                {
                    _cancellationTokenSource = null;
                }
                cancellationTokenSource?.Dispose();
            }
        }

        private void ProcessEventLoop(ArrayQueue<EventInfoModel> eventInfos, Process fixedProcess, CancellationToken cancellationToken)
        {
            ArrayQueue<Process> activeProcesses = new();
            List<Process> ownedProcesses = null;
            try
            {
                if (fixedProcess != null)
                {
                    activeProcesses.Add(fixedProcess);
                }
                else
                {
                    ownedProcesses = new();
                    IEnumerable<string> processNames = eventInfos.Select(eventInfo => eventInfo.ProcessInfo.ProcessName).Where(processName => string.IsNullOrEmpty(processName) == false).Distinct(StringComparer.OrdinalIgnoreCase);
                    foreach (string processName in processNames)
                    {
                        foreach (Process process in Process.GetProcessesByName(processName))
                        {
                            ownedProcesses.Add(process);
                            activeProcesses.Add(process);
                        }
                    }
                }
                if (activeProcesses.Count == 0)
                {
                    return;
                }
                while (cancellationToken.IsCancellationRequested == false)
                {
                    _macroModeController.Execute(activeProcesses, eventInfos, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                if (cancellationToken.IsCancellationRequested == false)
                {
                    throw;
                }
            }
            finally
            {
                if (ownedProcesses != null)
                {
                    foreach (Process process in ownedProcesses)
                    {
                        process.Dispose();
                    }
                }
            }
        }
    }
}
