using System;
using System.Windows;

namespace Macro.View
{
    /// <summary>
    /// CloseButtonWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CloseButtonWindow : Window
    {
        private Action _onCloseCallback;
        public CloseButtonWindow(Action onCloseCallback)
        {
            InitializeComponent();
            this.Loaded += CloseButtonWindow_Loaded;
            _onCloseCallback = onCloseCallback;
        }
        public void SetOwner(Window owner)
        {
            this.Owner = owner;
        }

        private void CloseButtonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Owner.LocationChanged += MainWindow_LocationChanged;
            this.Owner.SizeChanged += MainWindow_SizeChanged;
            this.Owner.Closed += MainWindow_Closed;
            MainWindow_LocationChanged(null, null);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            this.Owner.LocationChanged -= MainWindow_LocationChanged;
            this.Owner.SizeChanged -= MainWindow_SizeChanged;
            this.Owner.Closed -= MainWindow_Closed;
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
            var mainWindowPosition = this.Owner.PointToScreen(new Point(0, 0));

            this.Left = mainWindowPosition.X + this.Owner.ActualWidth - this.Width;
            this.Top = mainWindowPosition.Y;
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            _onCloseCallback?.Invoke();
        }
    }
}
