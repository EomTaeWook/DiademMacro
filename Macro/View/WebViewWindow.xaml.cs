using Dignus.Log;
using MahApps.Metro.Controls;
using System;
using System.Threading.Tasks;

namespace Macro.View
{
    /// <summary>
    /// WebViewControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WebViewWindow : MetroWindow
    {
        public WebViewWindow()
        {
            InitializeComponent();
        }
        public async Task LoadUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }
            try
            {
                if (webViewControl.CoreWebView2 == null)
                {
                    await webViewControl.EnsureCoreWebView2Async();
                }
                webViewControl.CoreWebView2.Navigate(url);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }
    }
}