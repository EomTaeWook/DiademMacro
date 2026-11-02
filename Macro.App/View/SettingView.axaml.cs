using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Interface;
using Macro.Infrastructure.Manager;
using Macro.Infrastructure.Mcp;
using Macro.Models;
using Macro.Models.ViewModel;
using Macro.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Threading;
using Dignus.Log;
using Utils;
using Utils.Models;

namespace Macro.View
{
    /// <summary>
    /// SettingView.xaml에 대한 상호 작용 논리
    /// </summary>
    public class SettingView : UIItem
    {
        internal Avalonia.Controls.ComboBox comboLanguage;
        internal Avalonia.Controls.TextBox txtSavePath;
        private Button _browseSavePathButton;
        private Button _openLogFolderButton;
        private Button _openMcpButton;
        private Button _closeMcpButton;
        private TextBlock _mcpOpenStatusText;
        private TextBlock _mcpClosedStatusText;
        private TextBox _mcpEndpointTextBox;
        private TextBox _mcpPortTextBox;
        private MacroMcpService _mcpService;
        private IDesktopPlatform _desktopPlatform;
        private Border _macPermissionsCard;
        private Button _requestInputPermissionButton;
        private Button _requestCapturePermissionButton;
        private TextBlock _inputPermissionGrantedText;
        private TextBlock _inputPermissionRequiredText;
        private TextBlock _capturePermissionGrantedText;
        private TextBlock _capturePermissionRequiredText;
        internal Avalonia.Controls.NumericUpDown numPeriod;
        internal Avalonia.Controls.NumericUpDown numDelay;
        internal Avalonia.Controls.NumericUpDown numSimilarity;
        internal Avalonia.Controls.NumericUpDown numDragDelay;
        internal Avalonia.Controls.ComboBox comboSearchResultDisplay;
        internal Avalonia.Controls.NumericUpDown numProcessLocationX;
        internal Avalonia.Controls.NumericUpDown numProcessLocationY;
        internal Avalonia.Controls.ComboBox comboMacroMode;
        internal Avalonia.Controls.Button btnSave;
        internal Avalonia.Controls.Button btnClose;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            comboLanguage = this.FindControl<Avalonia.Controls.ComboBox>("comboLanguage");
            txtSavePath = this.FindControl<Avalonia.Controls.TextBox>("txtSavePath");
            _browseSavePathButton = this.FindControl<Button>("browseSavePathButton");
            _openLogFolderButton = this.FindControl<Button>("openLogFolderButton");
            _openMcpButton = this.FindControl<Button>("openMcpButton");
            _closeMcpButton = this.FindControl<Button>("closeMcpButton");
            _mcpOpenStatusText = this.FindControl<TextBlock>("mcpOpenStatusText");
            _mcpClosedStatusText = this.FindControl<TextBlock>("mcpClosedStatusText");
            _mcpEndpointTextBox = this.FindControl<TextBox>("mcpEndpointTextBox");
            _mcpPortTextBox = this.FindControl<TextBox>("mcpPortTextBox");
            _macPermissionsCard = this.FindControl<Border>("macPermissionsCard");
            _requestInputPermissionButton = this.FindControl<Button>("requestInputPermissionButton");
            _requestCapturePermissionButton = this.FindControl<Button>("requestCapturePermissionButton");
            _inputPermissionGrantedText = this.FindControl<TextBlock>("inputPermissionGrantedText");
            _inputPermissionRequiredText = this.FindControl<TextBlock>("inputPermissionRequiredText");
            _capturePermissionGrantedText = this.FindControl<TextBlock>("capturePermissionGrantedText");
            _capturePermissionRequiredText = this.FindControl<TextBlock>("capturePermissionRequiredText");
            numPeriod = this.FindControl<Avalonia.Controls.NumericUpDown>("numPeriod");
            numDelay = this.FindControl<Avalonia.Controls.NumericUpDown>("numDelay");
            numSimilarity = this.FindControl<Avalonia.Controls.NumericUpDown>("numSimilarity");
            numDragDelay = this.FindControl<Avalonia.Controls.NumericUpDown>("numDragDelay");
            comboSearchResultDisplay = this.FindControl<Avalonia.Controls.ComboBox>("comboSearchResultDisplay");
            numProcessLocationX = this.FindControl<Avalonia.Controls.NumericUpDown>("numProcessLocationX");
            numProcessLocationY = this.FindControl<Avalonia.Controls.NumericUpDown>("numProcessLocationY");
            comboMacroMode = this.FindControl<Avalonia.Controls.ComboBox>("comboMacroMode");
            btnSave = this.FindControl<Avalonia.Controls.Button>("btnSave");
            btnClose = this.FindControl<Avalonia.Controls.Button>("btnClose");
            numPeriod.Minimum = ConstHelper.MinPeriod;
            numDelay.Minimum = ConstHelper.MinItemDelay;
            numSimilarity.Minimum = ConstHelper.MinSimilarity;
            numDragDelay.Minimum = ConstHelper.MinDragDelay;
        }

        private AppUpdateViewModel _updates;
        private bool _closeAllowed;
        private Task _prepareCloseTask;

        public SettingView()
        {
            InitializeComponent();
            Opened += SettingView_Loaded;
        }

        protected override async void OnClosing(WindowClosingEventArgs eventArgs)
        {
            if (_closeAllowed == true)
            {
                base.OnClosing(eventArgs);
                return;
            }
            eventArgs.Cancel = true;
            await PrepareForShutdownAsync();
            Close();
        }

        public Task PrepareForShutdownAsync()
        {
            if (_prepareCloseTask == null)
            {
                _prepareCloseTask = PrepareCloseCoreAsync();
            }
            return _prepareCloseTask;
        }

        private async Task PrepareCloseCoreAsync()
        {
            IsEnabled = false;
            UIManager.Instance.ButtonActionStateChanged -= UpdateEditState;
            Activated -= RefreshMacPermissions;
            if (_mcpService != null)
            {
                _mcpService.StateChanged -= McpStateChanged;
            }
            if (_updates != null)
            {
                await _updates.CloseAsync();
                _updates.PropertyChanged -= UpdateStateChanged;
            }
            _closeAllowed = true;
        }

        private void UpdateStateChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            UpdateEditState();
        }

        private void UpdateEditState()
        {
            bool canEdit = _updates.IsInstalling == false;
            if (UIManager.Instance.IsProcessingButtonActions == true)
            {
                canEdit = false;
            }
            if (_prepareCloseTask != null)
            {
                canEdit = false;
            }
            comboLanguage.IsEnabled = canEdit;
            txtSavePath.IsEnabled = canEdit;
            _browseSavePathButton.IsEnabled = canEdit;
            _openLogFolderButton.IsEnabled = canEdit;
            _openMcpButton.IsEnabled = canEdit;
            _closeMcpButton.IsEnabled = canEdit;
            _mcpPortTextBox.IsEnabled = canEdit;
            if (_mcpService.IsRunning == true)
            {
                _openMcpButton.IsEnabled = false;
                _mcpPortTextBox.IsEnabled = false;
            }
            else
            {
                _closeMcpButton.IsEnabled = false;
            }
            numPeriod.IsEnabled = canEdit;
            numDelay.IsEnabled = canEdit;
            numSimilarity.IsEnabled = canEdit;
            numDragDelay.IsEnabled = canEdit;
            comboSearchResultDisplay.IsEnabled = canEdit;
            numProcessLocationX.IsEnabled = canEdit;
            numProcessLocationY.IsEnabled = canEdit;
            comboMacroMode.IsEnabled = canEdit;
            btnSave.IsEnabled = canEdit;
            _requestInputPermissionButton.IsEnabled = canEdit;
            _requestCapturePermissionButton.IsEnabled = canEdit;
            RefreshMacPermissions(this, EventArgs.Empty);
        }
        private void Init()
        {
            var languages = Enum.GetValues(typeof(LanguageType)).Cast<LanguageType>().Where(r => r != LanguageType.Max);
            comboLanguage.ItemsSource = languages;

            var macroModeValues = Enum.GetValues(typeof(MacroModeType)).Cast<MacroModeType>();
            comboMacroMode.ItemsSource = macroModeValues;

            DataContext = ServiceResolver.GetService<SettingViewModel>();
            _updates = ServiceResolver.GetService<AppUpdateViewModel>();
            _mcpService = ServiceResolver.GetService<MacroMcpService>();
            _desktopPlatform = ServiceResolver.GetService<IDesktopPlatform>();
            _macPermissionsCard.IsVisible = OperatingSystem.IsMacOS();
            Activated += RefreshMacPermissions;
            RefreshMacPermissions(this, EventArgs.Empty);
            _mcpService.StateChanged += McpStateChanged;
            this.FindControl<AppUpdateView>("appUpdateView").DataContext = _updates;
            _updates.PropertyChanged += UpdateStateChanged;
            UIManager.Instance.ButtonActionStateChanged += UpdateEditState;
            UpdateMcpState();
        }
        private void SettingView_Loaded(object sender, EventArgs e)
        {
            EventInit();
            Init();
        }
        private void EventInit()
        {
            btnSave.Click += Button_Click;
            btnClose.Click += Button_Click;
            _openLogFolderButton.Click += Button_Click;
            _openMcpButton.Click += Button_Click;
            _closeMcpButton.Click += Button_Click;
            _requestInputPermissionButton.Click += Button_Click;
            _requestCapturePermissionButton.Click += Button_Click;
        }

        private void Button_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (sender == btnSave)
            {
                UIManager.Instance.EnqueueButtonAction(SaveConfigAsync);
            }
            else if (sender == btnClose)
            {
                _ = PrepareForShutdownAsync();
                UIManager.Instance.EnqueueButtonAction(Close);
            }
            else if (sender == _openLogFolderButton)
            {
                UIManager.Instance.EnqueueButtonAction(OpenLogFolder);
            }
            else if (sender == _openMcpButton)
            {
                UIManager.Instance.EnqueueButtonAction(() => SetMcpOpenAsync(true));
            }
            else if (sender == _closeMcpButton)
            {
                UIManager.Instance.EnqueueButtonAction(() => SetMcpOpenAsync(false));
            }
            else if (sender == _requestInputPermissionButton)
            {
                UIManager.Instance.EnqueueButtonAction(() =>
                {
                    _desktopPlatform.RequestInputPermission();
                    RefreshMacPermissions(this, EventArgs.Empty);
                });
            }
            else if (sender == _requestCapturePermissionButton)
            {
                UIManager.Instance.EnqueueButtonAction(() =>
                {
                    _desktopPlatform.RequestCapturePermission();
                    RefreshMacPermissions(this, EventArgs.Empty);
                });
            }
        }

        private void RefreshMacPermissions(object sender, EventArgs eventArgs)
        {
            if (OperatingSystem.IsMacOS() == false)
            {
                return;
            }
            if (_desktopPlatform == null)
            {
                return;
            }
            if (_prepareCloseTask != null)
            {
                return;
            }
            bool inputAllowed = _desktopPlatform.HasInputPermission;
            bool captureAllowed = _desktopPlatform.HasCapturePermission;
            bool canRequestPermission = UIManager.Instance.IsProcessingButtonActions == false;
            if (_updates.IsInstalling == true)
            {
                canRequestPermission = false;
            }
            _requestInputPermissionButton.IsEnabled = canRequestPermission;
            _requestCapturePermissionButton.IsEnabled = canRequestPermission;
            _inputPermissionGrantedText.IsVisible = inputAllowed;
            _inputPermissionRequiredText.IsVisible = inputAllowed == false;
            _capturePermissionGrantedText.IsVisible = captureAllowed;
            _capturePermissionRequiredText.IsVisible = captureAllowed == false;
            if (inputAllowed == true)
            {
                _requestInputPermissionButton.IsEnabled = false;
            }
            if (captureAllowed == true)
            {
                _requestCapturePermissionButton.IsEnabled = false;
            }
        }

        private void McpStateChanged()
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (_prepareCloseTask != null)
                {
                    return;
                }
                UpdateMcpState();
            });
        }

        private void UpdateMcpState()
        {
            bool isRunning = _mcpService.IsRunning;
            if (isRunning == true)
            {
                this.DataContext<SettingViewModel>().Config.McpPort = _mcpService.Port;
            }
            _mcpOpenStatusText.IsVisible = isRunning;
            _mcpClosedStatusText.IsVisible = isRunning == false;
            _mcpEndpointTextBox.Text = _mcpService.Endpoint;
            UpdateEditState();
        }

        private async Task SetMcpOpenAsync(bool isOpen)
        {
            if (_prepareCloseTask != null)
            {
                return;
            }
            if (_updates.IsInstalling == true)
            {
                return;
            }
            int failureTemplateId = 1095;
            try
            {
                if (isOpen == true)
                {
                    if (TryReadMcpPort(out int mcpPort) == false)
                    {
                        await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<MessageTemplate>.Find(1049).GetString());
                        return;
                    }
                    this.DataContext<SettingViewModel>().Config.McpPort = mcpPort;
                    await _mcpService.StartAsync(mcpPort);
                }
                else
                {
                    failureTemplateId = 1096;
                    await _mcpService.StopAsync();
                }
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<LabelTemplate>.Find(failureTemplateId).GetString() + Environment.NewLine + exception.Message);
            }
            finally
            {
                UpdateMcpState();
            }
        }

        private bool TryReadMcpPort(out int mcpPort)
        {
            if (int.TryParse(_mcpPortTextBox.Text, out mcpPort) == false)
            {
                return false;
            }
            if (mcpPort < 1024)
            {
                return false;
            }
            if (mcpPort > 65535)
            {
                return false;
            }
            return true;
        }

        private static void OpenLogFolder()
        {
            string logDirectoryPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "logs"));
            Directory.CreateDirectory(logDirectoryPath);
            Process.Start(new ProcessStartInfo(logDirectoryPath) { UseShellExecute = true });
        }

        private async Task SaveConfigAsync()
        {
            if (_prepareCloseTask != null)
            {
                return;
            }
            if (_updates.IsInstalling == true)
            {
                return;
            }
            Config configuration = this.DataContext<SettingViewModel>().Config;
            if (TryReadMcpPort(out int mcpPort) == false)
            {
                await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<MessageTemplate>.Find(1049).GetString());
                return;
            }
            configuration.McpPort = mcpPort;
            if (TryModelValidate(configuration, out MessageTemplate messageTemplate) == false)
            {
                await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), messageTemplate.GetString());
                return;
            }
            if (Save(configuration) == false)
            {
                await ApplicationManager.ShowMessageDialog(TemplateContainer<LabelTemplate>.Find(1062).GetString(), TemplateContainer<MessageTemplate>.Find(1015).GetString());
                return;
            }
            UIManager.Instance.ClosePopup(this);
        }

        private bool Save(Config model)
        {
            string path = Path.Combine(Environment.CurrentDirectory, ConstHelper.DefaultConfigFile);
            FileService fileService = ServiceResolver.GetService<FileService>();
            if (fileService.SaveJson(path, model) == false)
            {
                return false;
            }
            Config appliedConfiguration = this.DataContext<SettingViewModel>().ApplyChanges();
            NotifyHelper.InvokeNotify(NotifyEventType.ConfigChanged, new ConfigEventArgs { Config = appliedConfiguration });
            return true;
        }
        private bool TryModelValidate(Config model, out MessageTemplate messageTemplate)
        {
            messageTemplate = TemplateContainer<MessageTemplate>.Find(1000);

            if (model.ProcessPeriod < ConstHelper.MinPeriod)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1008);
                return false;
            }
            if (model.ItemDelay < ConstHelper.MinItemDelay)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1009);
                return false;
            }
            if (model.Similarity < ConstHelper.MinSimilarity)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1010);
                return false;
            }
            return true;
        }
    }
}
