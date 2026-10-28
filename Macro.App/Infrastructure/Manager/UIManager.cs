using Avalonia;
using Avalonia.Controls;
using Dignus.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Macro.View;

namespace Macro.Infrastructure.Manager
{
    public class UIManager : Singleton<UIManager>
    {
        private readonly List<Window> _activePopup = new();

        public void AddPopup<T>() where T : Window, new()
        {
            foreach (Window popup in _activePopup)
            {
                if (popup is T)
                {
                    popup.Activate();
                    return;
                }
            }
            Window window = new T();
            _activePopup.Add(window);
            window.Closed += (sender, eventArgs) => _activePopup.Remove(window);
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show(((App)Application.Current).MainWindow);
        }

        public async Task PrepareForShutdownAsync()
        {
            foreach (Window popup in _activePopup.ToArray())
            {
                if (popup is SettingView settings)
                {
                    await settings.PrepareForShutdownAsync();
                }
                popup.Close();
            }
        }
        public void ClosePopup(Window popup)
        {
            popup.Close();
        }
    }
}
