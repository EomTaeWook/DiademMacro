using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Dignus.Framework;
using Dignus.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Macro.View;

namespace Macro.Infrastructure.Manager
{
    public class UIManager : Singleton<UIManager>
    {
        private readonly List<Window> _activePopup = new();
        private readonly Queue<Func<Task>> _buttonActions = new();
        private Task _buttonActionTask = Task.CompletedTask;
        private bool _isProcessingButtonActions;
        private bool _acceptingButtonActions = true;

        public event Action ButtonActionStateChanged;

        public bool IsProcessingButtonActions
        {
            get
            {
                return _isProcessingButtonActions;
            }
        }

        public bool EnqueueButtonAction(Action buttonAction)
        {
            return EnqueueButtonAction(() =>
            {
                buttonAction();
                return Task.CompletedTask;
            });
        }

        public bool EnqueueButtonAction(Func<Task> buttonAction)
        {
            Dispatcher.UIThread.VerifyAccess();
            if (_acceptingButtonActions == false)
            {
                return false;
            }
            _buttonActions.Enqueue(buttonAction);
            if (_isProcessingButtonActions == true)
            {
                return true;
            }
            _isProcessingButtonActions = true;
            ButtonActionStateChanged?.Invoke();
            _buttonActionTask = ProcessButtonActionsAsync();
            return true;
        }

        private async Task ProcessButtonActionsAsync()
        {
            try
            {
                await Task.Yield();
                while (_buttonActions.Count > 0)
                {
                    Func<Task> buttonAction = _buttonActions.Dequeue();
                    try
                    {
                        await buttonAction();
                    }
                    catch (Exception exception)
                    {
                        LogHelper.Error(exception);
                    }
                }
            }
            finally
            {
                _isProcessingButtonActions = false;
                ButtonActionStateChanged?.Invoke();
            }
        }

        public Task StopButtonActionsAsync()
        {
            Dispatcher.UIThread.VerifyAccess();
            _acceptingButtonActions = false;
            return _buttonActionTask;
        }

        public void AddPopup<T>() where T : Window, new()
        {
            if (_acceptingButtonActions == false)
            {
                return;
            }
            foreach (Window popup in _activePopup)
            {
                if (popup is T)
                {
                    popup.Activate();
                    return;
                }
            }
            Window window = new T();
            if (TrackPopup(window) == false)
            {
                return;
            }
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show(((App)Application.Current).MainWindow);
        }

        internal bool TrackPopup(Window window)
        {
            if (_acceptingButtonActions == false)
            {
                return false;
            }
            _activePopup.Add(window);
            window.Closed += (sender, eventArgs) => _activePopup.Remove(window);
            return true;
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
