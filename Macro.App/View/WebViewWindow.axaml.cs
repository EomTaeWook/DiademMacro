using Avalonia.Controls;
using Dignus.Log;
using System;
using System.Threading.Tasks;

namespace Macro.View
{
    public class WebViewWindow : Window
    {
        internal Macro.UI.WebViewControl webViewControl;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            webViewControl = this.FindControl<Macro.UI.WebViewControl>("webViewControl");
        }

        public WebViewWindow()
        {
            InitializeComponent();
        }

        public Task LoadUrlAsync(string address)
        {
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
    }
}
