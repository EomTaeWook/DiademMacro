using Dignus.Log;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Macro.View
{
    /// <summary>
    /// WebViewControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WebViewControl : UserControl
    {
        public WebViewControl()
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