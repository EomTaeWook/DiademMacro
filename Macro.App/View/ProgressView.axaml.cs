using Avalonia.Controls;

namespace Macro.View
{
    public class ProgressView : Window
    {

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        public ProgressView()
        {
            InitializeComponent();
        }

        public void ShowProgressView()
        {
            Show(((App)Avalonia.Application.Current).MainWindow);
        }

        public void HideProgressView()
        {
            Hide();
        }
    }
}
