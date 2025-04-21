using Macro.UI;
using System;
using System.Windows;

namespace Macro.View
{
    /// <summary>
    /// WindowEventPopup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class WindowEventPopup : UIItem
    {
        public WindowEventPopup()
        {
            InitializeComponent();
            this.Loaded += WindowEventPopup_Loaded;
            this.Closed += WindowEventPopup_Closed;
        }

        private void WindowEventPopup_Closed(object sender, EventArgs e)
        {
            this.Loaded -= WindowEventPopup_Loaded;
            this.Closed -= WindowEventPopup_Closed;
        }

        private void WindowEventPopup_Loaded(object sender, RoutedEventArgs e)
        {
            InitEvent();
        }

        private void InitEvent()
        {

        }
    }
}
