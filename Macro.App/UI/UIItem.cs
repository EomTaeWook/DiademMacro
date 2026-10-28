using Avalonia.Controls;

namespace Macro.UI
{
    public abstract class UIItem : Window
    {
        protected UIItem()
        {
            WindowDecorations = WindowDecorations.None;
            ShowActivated = false;
            ShowInTaskbar = false;
            CanResize = false;
        }
    }
}
