using System;
using System.Windows;

namespace Macro.View
{
    /// <summary>
    /// CloseButtonWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CloseButtonWindowOld : Window
    {
        private readonly Window _mainWindow;
        private Action _onCloseCallback;
        public CloseButtonWindowOld(Window mainWindow,
            Action onCloseCallback)
        {
            InitializeComponent();
            this.Loaded += CloseButtonWindow_Loaded;
            this.Owner = mainWindow;
            _mainWindow = mainWindow;
            _onCloseCallback = onCloseCallback;
        }

        private void CloseButtonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindow.LocationChanged += MainWindow_LocationChanged;
            _mainWindow.SizeChanged += MainWindow_SizeChanged;
            _mainWindow.Closed += MainWindow_Closed;
            MainWindow_LocationChanged(null, null);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            _mainWindow.LocationChanged -= MainWindow_LocationChanged;
            _mainWindow.SizeChanged -= MainWindow_SizeChanged;
            _mainWindow.Closed -= MainWindow_Closed;
        }

        private void MainWindow_SizeChanged(object sender, EventArgs e)
        {
            UpdatePosition();
        }
        private void MainWindow_LocationChanged(object sender, EventArgs e)
        {
            UpdatePosition();
        }
        private void UpdatePosition()
        {
            var mainWindowPosition = _mainWindow.PointToScreen(new Point(0, 0));

            this.Left = mainWindowPosition.X + _mainWindow.ActualWidth - this.Width;
            this.Top = mainWindowPosition.Y;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            _onCloseCallback?.Invoke();
        }
    }
}
