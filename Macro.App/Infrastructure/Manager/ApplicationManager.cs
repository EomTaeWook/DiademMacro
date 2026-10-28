using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Threading;
using Dignus.Collections;
using Dignus.Framework;
using Macro.Infrastructure.Controller;
using Macro.View;
using System;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Manager
{
    public class ApplicationManager : Singleton<ApplicationManager>
    {
        private readonly ProgressView _progress = new();
        private readonly Window _mainWindow;
        private readonly ChildWindow _drawWindow = new();
        private readonly ArrayQueue<CaptureView> _captureViews = new();
        private readonly ArrayQueue<MousePositionView> _mouseInteractionViews = new();
        private IntPtr _drawWindowHandle;
        private ScreenCaptureManager _screenCaptureManager;

        public bool HasActiveInputOverlay
        {
            get
            {
                foreach (CaptureView view in _captureViews)
                {
                    if (view.IsVisible == true)
                    {
                        return true;
                    }
                }
                foreach (MousePositionView view in _mouseInteractionViews)
                {
                    if (view.IsVisible == true)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        public ApplicationManager()
        {
            _mainWindow = ((App)Application.Current).MainWindow;
        }

        public static async Task<bool> ShowMessageDialog(string title, string message, bool allowCancel = false)
        {
            Window dialog = new() { Title = title, Width = 420, SizeToContent = SizeToContent.Height, CanResize = false, WindowStartupLocation = WindowStartupLocation.CenterOwner, ShowInTaskbar = false };
            Button acceptButton = new() { Content = "OK", IsDefault = true, MinWidth = 80 };
            acceptButton.Click += (sender, eventArgs) => dialog.Close(true);
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            buttons.Children.Add(acceptButton);
            if (allowCancel == true)
            {
                Button cancelButton = new() { Content = "Cancel", IsCancel = true, MinWidth = 80 };
                cancelButton.Click += (sender, eventArgs) => dialog.Close(false);
                buttons.Children.Add(cancelButton);
            }
            StackPanel panel = new() { Margin = new Thickness(20), Spacing = 16 };
            panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap });
            panel.Children.Add(buttons);
            dialog.Content = panel;
            if (UIManager.Instance.TrackPopup(dialog) == false)
            {
                return false;
            }
            return await dialog.ShowDialog<bool>(Instance._mainWindow);
        }

        public static void ShowProgressbar()
        {
            Instance._progress.Width = Instance._mainWindow.Bounds.Width;
            Instance._progress.Height = Instance._mainWindow.Bounds.Height;
            Instance._progress.Position = Instance._mainWindow.Position;
            if (Instance._progress.IsVisible == false)
            {
                Instance._progress.Show(Instance._mainWindow);
            }
        }

        public static void HideProgressbar()
        {
            Instance._progress.Hide();
        }

        public Window GetDrawWindow()
        {
            return _drawWindow;
        }

        public IntPtr GetDrawWindowHandle()
        {
            return _drawWindowHandle;
        }

        public void Init()
        {
            _screenCaptureManager = ServiceResolver.GetService<ScreenCaptureManager>();
            _drawWindow.Opacity = 0;
            _drawWindow.Show();
            _drawWindowHandle = _drawWindow.TryGetPlatformHandle().Handle;
            _screenCaptureManager.SetDrawWindowHandle(_drawWindowHandle);
            ResetMonitorViews();
        }

        private void ResetMonitorViews()
        {
            foreach (CaptureView view in _captureViews)
            {
                view.Close();
            }
            foreach (MousePositionView view in _mouseInteractionViews)
            {
                view.Close();
            }
            _captureViews.Clear();
            _mouseInteractionViews.Clear();
            foreach (var monitor in _screenCaptureManager.GetMonitorInfo())
            {
                _captureViews.Add(new CaptureView(monitor));
                _mouseInteractionViews.Add(new MousePositionView(monitor));
            }
        }

        public void ShowAndActivateMousePositionViews()
        {
            ResetMonitorViews();
            foreach (MousePositionView view in _mouseInteractionViews)
            {
                view.ShowAndActivate();
            }
        }

        public void CloseMousePositionViews()
        {
            foreach (MousePositionView view in _mouseInteractionViews)
            {
                view.Hide();
            }
        }

        public void ShowCaptureImageViews()
        {
            ResetMonitorViews();
            foreach (CaptureView view in _captureViews)
            {
                view.ShowActivate(CaptureModeType.ImageCapture);
            }
            _mainWindow.WindowState = WindowState.Minimized;
        }

        public void ShowSetROIViews()
        {
            ResetMonitorViews();
            foreach (CaptureView view in _captureViews)
            {
                view.ShowActivate(CaptureModeType.ROICapture);
            }
            _mainWindow.WindowState = WindowState.Minimized;
        }

        public void CloseCaptureView()
        {
            foreach (CaptureView view in _captureViews)
            {
                view.Hide();
            }
            _mainWindow.WindowState = WindowState.Normal;
        }

        public void Dispose()
        {
            _screenCaptureManager?.SetDrawWindowHandle(IntPtr.Zero);
            _drawWindow.Close();
            _progress.Close();
            CoroutineRunner.Dispose();
            foreach (CaptureView view in _captureViews)
            {
                view.Close();
            }
            foreach (MousePositionView view in _mouseInteractionViews)
            {
                view.Close();
            }
        }
    }
}
