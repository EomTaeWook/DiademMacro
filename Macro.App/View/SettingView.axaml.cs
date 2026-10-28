using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.Models.ViewModel;
using Macro.UI;
using System;
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
        internal Avalonia.Controls.NumericUpDown numPeriod;
        internal Avalonia.Controls.NumericUpDown numDelay;
        internal Avalonia.Controls.NumericUpDown numSimilarity;
        internal Avalonia.Controls.NumericUpDown numDragDelay;
        internal Avalonia.Controls.ComboBox comboSearchResultDisplay;
        internal Avalonia.Controls.NumericUpDown numProcessLocationX;
        internal Avalonia.Controls.NumericUpDown numProcessLocationY;
        internal Avalonia.Controls.ComboBox comboMacroMode;
        internal Avalonia.Controls.ComboBox comboProcessPreviewOpen;
        internal Avalonia.Controls.Button btnSave;
        internal Avalonia.Controls.Button btnClose;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            comboLanguage = this.FindControl<Avalonia.Controls.ComboBox>("comboLanguage");
            txtSavePath = this.FindControl<Avalonia.Controls.TextBox>("txtSavePath");
            _browseSavePathButton = this.FindControl<Button>("browseSavePathButton");
            numPeriod = this.FindControl<Avalonia.Controls.NumericUpDown>("numPeriod");
            numDelay = this.FindControl<Avalonia.Controls.NumericUpDown>("numDelay");
            numSimilarity = this.FindControl<Avalonia.Controls.NumericUpDown>("numSimilarity");
            numDragDelay = this.FindControl<Avalonia.Controls.NumericUpDown>("numDragDelay");
            comboSearchResultDisplay = this.FindControl<Avalonia.Controls.ComboBox>("comboSearchResultDisplay");
            numProcessLocationX = this.FindControl<Avalonia.Controls.NumericUpDown>("numProcessLocationX");
            numProcessLocationY = this.FindControl<Avalonia.Controls.NumericUpDown>("numProcessLocationY");
            comboMacroMode = this.FindControl<Avalonia.Controls.ComboBox>("comboMacroMode");
            comboProcessPreviewOpen = this.FindControl<Avalonia.Controls.ComboBox>("comboProcessPreviewOpen");
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
        public Config Config { get; private set; }

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
            if (_updates != null)
            {
                await _updates.CloseAsync();
                _updates.PropertyChanged -= UpdateStateChanged;
            }
            _closeAllowed = true;
        }

        private void UpdateStateChanged(object sender, PropertyChangedEventArgs eventArgs)
        {
            bool canEdit = _updates.IsInstalling == false;
            comboLanguage.IsEnabled = canEdit;
            txtSavePath.IsEnabled = canEdit;
            _browseSavePathButton.IsEnabled = canEdit;
            numPeriod.IsEnabled = canEdit;
            numDelay.IsEnabled = canEdit;
            numSimilarity.IsEnabled = canEdit;
            numDragDelay.IsEnabled = canEdit;
            comboSearchResultDisplay.IsEnabled = canEdit;
            numProcessLocationX.IsEnabled = canEdit;
            numProcessLocationY.IsEnabled = canEdit;
            comboMacroMode.IsEnabled = canEdit;
            comboProcessPreviewOpen.IsEnabled = canEdit;
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
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn.Equals(btnSave))
            {
                var model = (DataContext as SettingViewModel).Config;
                if (TryModelValidate(model, out MessageTemplate template))
                {
                    Save(model);
                }
                else
                {
                    await ApplicationManager.ShowMessageDialog("Error", template.GetString());
                }
                UIManager.Instance.ClosePopup(this);
            }
            else if (btn.Equals(btnClose))
            {
                UIManager.Instance.ClosePopup(this);
            }
        }
        private void Save(Config model)
        {
            var path = Path.Combine(Environment.CurrentDirectory, ConstHelper.DefaultConfigFile);
            var fileService = ServiceResolver.GetService<FileService>();
            var saved = fileService.SaveJson(path, model);

            if (saved == true)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.ConfigChanged, new ConfigEventArgs() { Config = model });
            }
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
