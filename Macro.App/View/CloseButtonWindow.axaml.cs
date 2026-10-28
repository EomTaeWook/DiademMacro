using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System;

namespace Macro.View
{
    public class CloseButtonWindow : Window
    {
        internal Avalonia.Controls.Button btnClose;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            btnClose = this.FindControl<Avalonia.Controls.Button>("btnClose");
        }

        private readonly Action _onCloseCallback;
        private Window _webWindow;

        public CloseButtonWindow(Action onCloseCallback)
        {
            InitializeComponent();
            _onCloseCallback = onCloseCallback;
            Closed += Window_Closed;
        }

        public void SetOwner(Window owner)
        {
            if (_webWindow != null)
            {
                _webWindow.PositionChanged -= Owner_PositionChanged;
                _webWindow.SizeChanged -= Owner_SizeChanged;
            }
            _webWindow = owner;
            _webWindow.PositionChanged += Owner_PositionChanged;
            _webWindow.SizeChanged += Owner_SizeChanged;
            UpdatePosition();
        }

        private void Owner_PositionChanged(object sender, PixelPointEventArgs eventArgs)
        {
            UpdatePosition();
        }

        private void Owner_SizeChanged(object sender, SizeChangedEventArgs eventArgs)
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Position = new PixelPoint(_webWindow.Position.X + (int)((_webWindow.Bounds.Width - Width) * _webWindow.RenderScaling), _webWindow.Position.Y);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs eventArgs)
        {
            Hide();
            _onCloseCallback?.Invoke();
        }

        private void Window_Closed(object sender, EventArgs eventArgs)
        {
            if (_webWindow != null)
            {
                _webWindow.PositionChanged -= Owner_PositionChanged;
                _webWindow.SizeChanged -= Owner_SizeChanged;
            }
        }
    }
}
