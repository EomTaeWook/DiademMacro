using Avalonia.Controls;
using Avalonia.Platform;
using System;
using Utils.Platforms.MacOS;

namespace Macro.UI
{
    public class WebViewControl : NativeControlHost
    {
        private IntPtr _webView;
        private string _address;

        public void Navigate(string address)
        {
            _address = address;
            if (_webView != IntPtr.Zero)
            {
                MacOSNative.NavigateWebView(_webView, address);
            }
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            _webView = MacOSNative.CreateWebView(parent.Handle);
            if (_webView == IntPtr.Zero)
            {
                throw new InvalidOperationException("The macOS web view could not be created.");
            }
            if (string.IsNullOrEmpty(_address) == false)
            {
                MacOSNative.NavigateWebView(_webView, _address);
            }
            return new PlatformHandle(_webView, "NSView");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            if (_webView != IntPtr.Zero)
            {
                MacOSNative.DestroyWebView(_webView);
                _webView = IntPtr.Zero;
            }
        }
    }
}
