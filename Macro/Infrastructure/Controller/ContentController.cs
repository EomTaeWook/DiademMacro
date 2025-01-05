using DataContainer.Generated;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.View;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using TemplateContainers;

namespace Macro.Infrastructure.Controller
{

    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class ContentController
    {
        private ContentView _contentView;
        private CancellationTokenSource _cts;
        private Process _fixedProcess;
        private Config _config;

        private MacroModeControllerBase _macroModeController;
        private CancellationToken _cancellationToken;
        public ContentController(Config config, InputEventProcessorHandler eventProcessorHandler)
        {
            _config = config;
            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
            InitializeController();
        }

        private void InitializeController()
        {
            if (_config.MacroMode == MacroModeType.SequentialMode)
            {
                _macroModeController = ServiceDispatcher.Resolve<SequentialModeController>();
            }
            else
            {
                _macroModeController = ServiceDispatcher.Resolve<BatchModeController>();
            }
        }

        private void NotifyHelper_ConfigChanged(ConfigEventArgs obj)
        {
            _config = obj.Config;
            InitializeController();
        }

        public void SetContentView(ContentView baseContentView)
        {
            this._contentView = baseContentView;
        }

        public bool Validate(EventTriggerModel model, out MessageTemplate messageTemplate)
        {
            messageTemplate = TemplateContainer<MessageTemplate>.Find(1000);

            model.KeyboardCmd = model.KeyboardCmd.Replace(" ", "");

            if (model.Image == null)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1001);
                return false;
            }
            if (model.EventType == EventType.Mouse && model.MouseTriggerInfo.MouseInfoEventType == MouseEventType.None)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1002);
                return false;
            }

            if (string.IsNullOrEmpty(model.KeyboardCmd) && model.EventType == EventType.Keyboard)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1003);
                return false;
            }
            if (string.IsNullOrEmpty(model.ProcessInfo.ProcessName))
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1004);
                return false;
            }

            return true;
        }
        public void Start()
        {
            if (_cts == null)
            {
                _cts = new CancellationTokenSource();
                _cancellationToken = _cts.Token;
            }

            var _ = Task.Run(() => ProcessEventLoop());
        }

        private void ProcessEventLoop()
        {
            ArrayQueue<EventTriggerModel> eventTriggers = new ArrayQueue<EventTriggerModel>();

            foreach (var item in _contentView.eventSettingView.GetDataContext().TriggerSaves)
            {
                if (item.IsChecked)
                {
                    eventTriggers.Add(item);
                }
            }
            ArrayQueue<Process> activeProcesses = new ArrayQueue<Process>();

            if (_fixedProcess != null)
            {
                activeProcesses.Add(_fixedProcess);
            }
            else
            {
                UniqueSet<Process> uniqueProcesses = new UniqueSet<Process>();

                foreach (var item in eventTriggers)
                {
                    var processInfos = Process.GetProcessesByName(item.ProcessInfo.ProcessName);
                    uniqueProcesses.AddRange(processInfos);
                }

                foreach (var process in uniqueProcesses)
                {
                    activeProcesses.Add(process);
                }
            }

            _macroModeController.SetDrawImageCallback(OnRenderCaptureImage);

            while (_cancellationToken.IsCancellationRequested == false)
            {
                _macroModeController.Execute(
                    activeProcesses,
                    eventTriggers,
                    _cancellationToken);
            }
        }
        private void OnRenderCaptureImage(Bitmap bitmap)
        {
            _contentView.DrawCaptureImage(bitmap);
        }
        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
        }
        public void SetFixProcess(Process process)
        {
            this._fixedProcess = process;
        }
    }
}
