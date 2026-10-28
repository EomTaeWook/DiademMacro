using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Panel = System.Windows.Forms.Panel;

namespace Macro.UI
{
    public class WebViewControl : NativeControlHost
    {
        private Panel _panel;
        private WebBrowser _browser;
        private string _address;

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr childHandle, IntPtr parentHandle);

        public void Navigate(string address)
        {
            _address = address;
            if (_browser != null)
            {
                _browser.Navigate(address);
            }
        }

        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
        {
            _panel = new Panel();
            _browser = new WebBrowser { Dock = DockStyle.Fill, ScriptErrorsSuppressed = true };
            _panel.Controls.Add(_browser);
            IntPtr panelHandle = _panel.Handle;
            SetParent(panelHandle, parent.Handle);
            if (string.IsNullOrEmpty(_address) == false)
            {
                _browser.Navigate(_address);
            }
            return new PlatformHandle(panelHandle, "HWND");
        }

        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            _panel.Dispose();
            _panel = null;
            _browser = null;
        }
    }
}
