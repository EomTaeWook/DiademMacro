using Dignus.Collections;
using Dignus.Framework;
using Macro.View;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Macro.Infrastructure.Manager
{
    public class ApplicationManager : Singleton<ApplicationManager>
    {
        public static MessageDialogResult ShowMessageDialog(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative)
        {
            return Instance._mainWindow.ShowModalMessageExternal(title, message, style, new MetroDialogSettings()
            {
                ColorScheme = MetroDialogColorScheme.Inverted,
            });
        }
        public static void ShowProgressbar()
        {
            Instance._mainWindow.Dispatcher.Invoke(() =>
            {
                Instance._progress.Width = Instance._mainWindow.ActualWidth;
                Instance._progress.Height = Instance._mainWindow.ActualHeight;

                Instance._progress.Left = Instance._mainWindow.Left;
                Instance._progress.Top = Instance._mainWindow.Top;
                Instance._progress.Show();
            });
        }
        public static void HideProgressbar()
        {
            Instance._progress.Hide();
        }

        private readonly ProgressView _progress;

        private readonly MetroWindow _mainWindow;

        private readonly ChildWindow _drawWindow = new ChildWindow();

        private readonly ArrayQueue<CaptureView> _captureViews = new ArrayQueue<CaptureView>();
        private readonly ArrayQueue<MousePositionView> _mousePointViews = new ArrayQueue<MousePositionView>();
        private IntPtr _drawWindowHandle;
        private ScreenCaptureManager _screenCaptureManager;
        public ApplicationManager()
        {
            _mainWindow = Application.Current.MainWindow as MetroWindow;
            _progress = new ProgressView
            {
                Owner = _mainWindow,
                RenderSize = _mainWindow.RenderSize
            };
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
            Application.Current.MainWindow.Unloaded += MainWindow_Unloaded;
            _screenCaptureManager = ServiceDispatcher.GetService<ScreenCaptureManager>();
            _drawWindow.Opacity = 0;
#if DEBUG
            _drawWindow.Opacity = 1;
            _drawWindow.Background = Brushes.White;
            _drawWindow.Left = 0;
            _drawWindow.Top = 0;
#endif
            _drawWindow.Show();
            ResetMonitorViews();

            _drawWindowHandle = new WindowInteropHelper(_drawWindow).Handle;
            SchedulerManager.Instance.Start();
        }
        private void ResetMonitorViews()
        {
            foreach (var item in _captureViews)
            {
                item.Close();
            }
            foreach (var item in _mousePointViews)
            {
                item.Close();
            }
            _captureViews.Clear();
            _mousePointViews.Clear();

            foreach (var item in _screenCaptureManager.GetMonitorInfo())
            {
                _captureViews.Add(new CaptureView(item));
                _mousePointViews.Add(new MousePositionView(item));
            }
        }
        private void MainWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Dispose();
        }
        public void ShowMousePointView()
        {
            foreach (var item in _mousePointViews)
            {
                item.ShowActivate();
            }
        }
        public void CloseMousePointView()
        {
            foreach (var item in _mousePointViews)
            {
                item.Hide();
            }
        }

        public void ShowCaptureImageView()
        {
            ResetMonitorViews();

            foreach (var item in _captureViews)
            {
                item.ShowActivate(CaptureModeType.ImageCapture);
            }
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }
        public void ShowSetROIView()
        {
            foreach (var item in _captureViews)
            {
                item.ShowActivate(CaptureModeType.ROICaptureCompleted);
            }
            Application.Current.MainWindow.WindowState = WindowState.Minimized;
        }

        public void CloseCaptureView()
        {
            foreach (var item in _captureViews)
            {
                item.Hide();
            }
            Application.Current.MainWindow.WindowState = WindowState.Normal;
        }

        public void Dispose()
        {
            _drawWindow.Close();
            SchedulerManager.Instance.Stop();
            _progress.Close();
            foreach (var item in _captureViews)
            {
                item.Close();
            }
            foreach (var item in _mousePointViews)
            {
                item.Close();
            }
        }
    }
}
