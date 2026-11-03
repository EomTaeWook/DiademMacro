using Avalonia.Controls;
using Avalonia.Interactivity;
using DataContainer.Generated;
using Dignus.Log;
using Macro.Extensions;
using Macro.Infrastructure;
using Macro.Infrastructure.Manager;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;

namespace Macro.View
{
    public class WebViewWindow : Window
    {
        internal Macro.UI.WebViewControl webViewControl;
        private Button _closeAdvertisementButton;
        private Button _advertisementDetailsButton;
        private TextBlock _countdownText;
        private TextBlock _advertisementReadyText;
        private ProgressBar _advertisementProgress;
        private Uri _advertisementAddress;
        private bool _closingForShutdown;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            webViewControl = this.FindControl<Macro.UI.WebViewControl>("webViewControl");
            _closeAdvertisementButton = this.FindControl<Button>("closeAdvertisementButton");
            _advertisementDetailsButton = this.FindControl<Button>("advertisementDetailsButton");
            _countdownText = this.FindControl<TextBlock>("countdownText");
            _advertisementReadyText = this.FindControl<TextBlock>("advertisementReadyText");
            _advertisementProgress = this.FindControl<ProgressBar>("advertisementProgress");
        }

        public WebViewWindow()
        {
            InitializeComponent();
        }

        public Task LoadUrlAsync(string address)
        {
            _advertisementAddress = null;
            if (Uri.TryCreate(address, UriKind.Absolute, out Uri advertisementAddress) == true)
            {
                if (advertisementAddress.Scheme == Uri.UriSchemeHttp)
                {
                    _advertisementAddress = advertisementAddress;
                }
                if (advertisementAddress.Scheme == Uri.UriSchemeHttps)
                {
                    _advertisementAddress = advertisementAddress;
                }
            }
            _advertisementDetailsButton.IsEnabled = _advertisementAddress != null;
            if (string.IsNullOrEmpty(address) == false)
            {
                try
                {
                    webViewControl.Navigate(address);
                }
                catch (Exception exception)
                {
                    LogHelper.Error(exception);
                }
            }
            return Task.CompletedTask;
        }

        public void PrepareAdvertisement(bool allowManualClose, TimeSpan advertisementDelay)
        {
            _closeAdvertisementButton.IsVisible = allowManualClose;
            _closeAdvertisementButton.IsEnabled = false;
            _advertisementDetailsButton.IsVisible = allowManualClose;
            _advertisementProgress.IsVisible = true;
            _countdownText.IsVisible = true;
            _advertisementReadyText.IsVisible = false;
            UpdateCountdown(advertisementDelay, 0);
        }

        public void UpdateCountdown(TimeSpan remainingTime, double progress)
        {
            string countdownFormat = TemplateContainer<LabelTemplate>.Find(1106).GetString();
            _countdownText.Text = string.Format(CultureInfo.CurrentCulture, countdownFormat, Math.Max(0, Math.Ceiling(remainingTime.TotalSeconds)));
            _advertisementProgress.Value = Math.Clamp(progress, 0, 1);
        }

        public void AllowAdvertisementClose()
        {
            _closeAdvertisementButton.IsEnabled = true;
            _advertisementProgress.Value = 1;
            _countdownText.IsVisible = false;
            _advertisementReadyText.IsVisible = true;
        }

        private void CloseAdvertisement_Click(object sender, RoutedEventArgs eventArgs)
        {
            if (_closeAdvertisementButton.IsEnabled == false)
            {
                return;
            }
            Hide();
        }

        private void AdvertisementDetails_Click(object sender, RoutedEventArgs eventArgs)
        {
            UIManager.Instance.EnqueueButtonAction(OpenAdvertisementDetails);
        }

        private void OpenAdvertisementDetails()
        {
            if (_advertisementAddress == null)
            {
                return;
            }
            Process.Start(new ProcessStartInfo(_advertisementAddress.AbsoluteUri) { UseShellExecute = true });
        }

        protected override void OnClosing(WindowClosingEventArgs eventArgs)
        {
            if (_closingForShutdown == false)
            {
                eventArgs.Cancel = true;
                if (_closeAdvertisementButton.IsEnabled == true)
                {
                    Hide();
                }
            }
            base.OnClosing(eventArgs);
        }

        public void CloseForShutdown()
        {
            _closingForShutdown = true;
            Close();
        }
    }
}
