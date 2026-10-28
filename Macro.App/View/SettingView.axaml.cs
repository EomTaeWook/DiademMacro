using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Manager;
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
            numPeriod.IsEnabled = canEdit;
            numDelay.IsEnabled = canEdit;
            numSimilarity.IsEnabled = canEdit;
            numDragDelay.IsEnabled = canEdit;
            comboSearchResultDisplay.IsEnabled = canEdit;
            numProcessLocationX.IsEnabled = canEdit;
            numProcessLocationY.IsEnabled = canEdit;
            comboMacroMode.IsEnabled = canEdit;
            btnSave.IsEnabled = canEdit;
        }
        private void Init()
        {
            var languages = Enum.GetValues(typeof(LanguageType)).Cast<LanguageType>().Where(r => r != LanguageType.Max);
            comboLanguage.ItemsSource = languages;

            var macroModeValues = Enum.GetValues(typeof(MacroModeType)).Cast<MacroModeType>();
            comboMacroMode.ItemsSource = macroModeValues;

            DataContext = ServiceResolver.GetService<SettingViewModel>();
            _updates = ServiceResolver.GetService<AppUpdateViewModel>();
            this.FindControl<AppUpdateView>("appUpdateView").DataContext = _updates;
            _updates.PropertyChanged += UpdateStateChanged;
            UIManager.Instance.ButtonActionStateChanged += UpdateEditState;
            UpdateEditState();
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
