using Avalonia;
using Avalonia.Controls;

namespace Macro.View
{
    public class ChildWindow : Window
    {

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
        }

        public ChildWindow()
        {
            InitializeComponent();
            Position = new PixelPoint(-32768, -32768);
        }
    }
}
