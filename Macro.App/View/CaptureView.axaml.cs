using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Dignus.Log;
using Macro.Infrastructure;
using Macro.Models;
using System;
using System.Drawing;
using Utils;
using Utils.Infrastructure;
using Point = Avalonia.Point;
using Brushes = Avalonia.Media.Brushes;

namespace Macro.View
{
    public class CaptureView : Window
    {
        internal Avalonia.Controls.Canvas captureZone;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            captureZone = this.FindControl<Avalonia.Controls.Canvas>("captureZone");
        }

        private bool _isDrag;
        private Point _originPoint;
        private readonly MonitorInfo _monitorInfo;
        private Border _dragBorder;
        private CaptureModeType _captureMode;

        public CaptureView(MonitorInfo monitorInfo)
        {
            _monitorInfo = monitorInfo;
            InitializeComponent();
            captureZone.PointerPressed += CaptureZone_PointerPressed;
            captureZone.PointerMoved += CaptureZone_PointerMoved;
            captureZone.PointerReleased += CaptureZone_PointerReleased;
            captureZone.PointerExited += CaptureZone_PointerExited;
            AddHandler(KeyDownEvent, CaptureView_KeyDown, RoutingStrategies.Tunnel);
#if !DEBUG
            Topmost = true;
#endif
        }

        public void ShowActivate(CaptureModeType captureModeType)
        {
            _captureMode = captureModeType;
            Position = new PixelPoint(_monitorInfo.Rect.Left, _monitorInfo.Rect.Top);
            Show();
            Width = _monitorInfo.Rect.Width / RenderScaling;
            Height = _monitorInfo.Rect.Height / RenderScaling;
            Position = new PixelPoint(_monitorInfo.Rect.Left, _monitorInfo.Rect.Top);
            Activate();
            Clear();
        }

        private void Clear()
        {
            _isDrag = false;
            captureZone.Children.Clear();
            IBrush borderBrush = Brushes.Blue;
            IBrush background = Brushes.LightBlue;
            if (_captureMode == CaptureModeType.ROICapture)
            {
                borderBrush = Brushes.Red;
                background = Brushes.LightPink;
            }
            _dragBorder = new Border { BorderBrush = borderBrush, BorderThickness = new Thickness(1), Background = background, CornerRadius = new CornerRadius(1), UseLayoutRounding = true, Width = 0, Height = 0 };
            captureZone.Children.Add(_dragBorder);
            WindowState = WindowState.Normal;
        }

        private void CaptureView_KeyDown(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Key != Key.Escape)
            {
                return;
            }
            _isDrag = false;
            if (_captureMode == CaptureModeType.ImageCapture)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.ScreenCaptureCompleted, new CaptureCompletedEventArgs());
            }
            else
            {
                NotifyHelper.InvokeNotify(NotifyEventType.ROICaptureCompleted, new ROICaptureCompletedEventArgs());
            }
            eventArgs.Handled = true;
        }

        private void CaptureZone_PointerPressed(object sender, PointerPressedEventArgs eventArgs)
        {
            if (eventArgs.GetCurrentPoint(captureZone).Properties.IsLeftButtonPressed == false)
            {
                return;
            }
            _isDrag = true;
            _originPoint = eventArgs.GetPosition(captureZone);
            eventArgs.Handled = true;
        }

        private void CaptureZone_PointerMoved(object sender, PointerEventArgs eventArgs)
        {
            if (_isDrag == false)
            {
                return;
            }
            if (eventArgs.GetCurrentPoint(captureZone).Properties.IsLeftButtonPressed == false)
            {
                return;
            }
            UpdateDragSelectionRect(_originPoint, eventArgs.GetPosition(captureZone));
            eventArgs.Handled = true;
        }

        private void UpdateDragSelectionRect(Point origin, Point current)
        {
            Canvas.SetLeft(_dragBorder, Math.Min(origin.X, current.X));
            Canvas.SetTop(_dragBorder, Math.Min(origin.Y, current.Y));
            _dragBorder.Width = Math.Abs(current.X - origin.X);
            _dragBorder.Height = Math.Abs(current.Y - origin.Y);
        }

        private Bitmap CaptureScreenRegion(IntRect rectangle)
        {
            try
            {
                Bitmap bitmap = new(rectangle.Width, rectangle.Height);
                using Graphics graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(_monitorInfo.Rect.Left + rectangle.Left, _monitorInfo.Rect.Top + rectangle.Top, 0, 0, new System.Drawing.Size(rectangle.Width, rectangle.Height), CopyPixelOperation.SourceCopy);
                return bitmap;
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                return null;
            }
        }

        private void CaptureZone_PointerReleased(object sender, PointerReleasedEventArgs eventArgs)
        {
            CompleteCapture(eventArgs.GetPosition(captureZone));
            eventArgs.Handled = true;
        }

        private void CaptureZone_PointerExited(object sender, PointerEventArgs eventArgs)
        {
            CompleteCapture(eventArgs.GetPosition(captureZone));
        }

        private void CompleteCapture(Point currentPoint)
        {
            if (_isDrag == false)
            {
                return;
            }
            if (IsVisible == false)
            {
                return;
            }
            _isDrag = false;
            UpdateDragSelectionRect(_originPoint, currentPoint);
            PixelPoint origin = captureZone.PointToScreen(new Point(Canvas.GetLeft(_dragBorder), Canvas.GetTop(_dragBorder)));
            PixelPoint end = captureZone.PointToScreen(new Point(Canvas.GetLeft(_dragBorder) + _dragBorder.Width, Canvas.GetTop(_dragBorder) + _dragBorder.Height));
            IntRect rectangle = new() { Left = origin.X - _monitorInfo.Rect.Left, Top = origin.Y - _monitorInfo.Rect.Top, Right = end.X - _monitorInfo.Rect.Left, Bottom = end.Y - _monitorInfo.Rect.Top };
            Hide();
            if (_captureMode == CaptureModeType.ImageCapture)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.ScreenCaptureCompleted, new CaptureCompletedEventArgs { MonitorInfo = _monitorInfo, CaptureImage = CaptureScreenRegion(rectangle), Position = rectangle });
            }
            else
            {
                System.Drawing.Point systemDpi = NativeHelper.GetSystemDPI();
                double horizontalFactor = (double)_monitorInfo.Dpi.X / systemDpi.X;
                double verticalFactor = (double)_monitorInfo.Dpi.Y / systemDpi.Y;
                IntRect roiRectangle = new() { Left = (int)Math.Round(rectangle.Left * horizontalFactor), Right = (int)Math.Round(rectangle.Right * horizontalFactor), Top = (int)Math.Round(rectangle.Top * verticalFactor), Bottom = (int)Math.Round(rectangle.Bottom * verticalFactor) };
                NotifyHelper.InvokeNotify(NotifyEventType.ROICaptureCompleted, new ROICaptureCompletedEventArgs { MonitorInfo = _monitorInfo, RoiRect = roiRectangle });
            }
        }
    }
}
