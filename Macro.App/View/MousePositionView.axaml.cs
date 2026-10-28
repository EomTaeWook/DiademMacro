using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Macro.Infrastructure;
using Macro.Models;
using System.Linq;
using Utils.Infrastructure;

namespace Macro.View
{
    public class MousePositionView : Window
    {
        internal Avalonia.Controls.Shapes.Path pathMousePoint;

        private void InitializeComponent()
        {
            Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(this);
            pathMousePoint = this.FindControl<Avalonia.Controls.Shapes.Path>("pathMousePoint");
        }

        private readonly MonitorInfo _monitorInfo;
        private bool _isDragging;
        private readonly PathFigure _mousePathFigure = new() { IsClosed = false, Segments = new PathSegments() };

        public MousePositionView(MonitorInfo monitorInfo)
        {
            _monitorInfo = monitorInfo;
            InitializeComponent();
            pathMousePoint.Data = new PathGeometry { Figures = [_mousePathFigure] };
            AddHandler(KeyDownEvent, MousePositionView_KeyDown, RoutingStrategies.Tunnel);
            AddHandler(PointerPressedEvent, MousePositionView_PointerPressed, RoutingStrategies.Tunnel);
            AddHandler(PointerMovedEvent, MousePositionView_PointerMoved, RoutingStrategies.Tunnel);
            AddHandler(PointerReleasedEvent, MousePositionView_PointerReleased, RoutingStrategies.Tunnel);
#if !DEBUG
            Topmost = true;
#endif
        }

        public void ShowAndActivate()
        {
            _mousePathFigure.Segments.Clear();
            _isDragging = false;
            Position = new PixelPoint(_monitorInfo.Rect.Left, _monitorInfo.Rect.Top);
            Show();
            Width = _monitorInfo.Rect.Width / RenderScaling;
            Height = _monitorInfo.Rect.Height / RenderScaling;
            Position = new PixelPoint(_monitorInfo.Rect.Left, _monitorInfo.Rect.Top);
            Activate();
        }

        private void MousePositionView_PointerPressed(object sender, PointerPressedEventArgs eventArgs)
        {
            if (IsVisible == false)
            {
                return;
            }
            _isDragging = false;
            _mousePathFigure.Segments.Clear();
            _mousePathFigure.StartPoint = eventArgs.GetPosition(this);
            eventArgs.Handled = true;
        }

        private void MousePositionView_PointerMoved(object sender, PointerEventArgs eventArgs)
        {
            if (IsVisible == false)
            {
                return;
            }
            if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed == false)
            {
                return;
            }
            _isDragging = true;
            _mousePathFigure.Segments.Add(new LineSegment { Point = eventArgs.GetPosition(this) });
            eventArgs.Handled = true;
        }

        private Point2D ToScreenPoint(Point point)
        {
            PixelPoint screenPoint = this.PointToScreen(point);
            return new Point2D(screenPoint.X, screenPoint.Y);
        }

        private void MousePositionView_PointerReleased(object sender, PointerReleasedEventArgs eventArgs)
        {
            if (IsVisible == false)
            {
                return;
            }
            MouseEventInfoV2 mouseEventInfo = new() { MouseEventType = MouseEventType.LeftClick, MousePoint = ToScreenPoint(eventArgs.GetPosition(this)) };
            if (eventArgs.InitialPressMouseButton == Avalonia.Input.MouseButton.Right)
            {
                if (_isDragging == true)
                {
                    _mousePathFigure.Segments.Clear();
                    _isDragging = false;
                    return;
                }
                mouseEventInfo.MouseEventType = MouseEventType.RightClick;
            }
            else if (_isDragging == true)
            {
                mouseEventInfo.MouseEventType = MouseEventType.Drag;
                mouseEventInfo.MousePoint = ToScreenPoint(_mousePathFigure.StartPoint);
                mouseEventInfo.MousePoints = _mousePathFigure.Segments.OfType<LineSegment>().Select(segment => ToScreenPoint(segment.Point)).ToList();
                mouseEventInfo.MousePoints.Add(ToScreenPoint(eventArgs.GetPosition(this)));
            }
            NotifyHelper.InvokeNotify(NotifyEventType.MouseInteractionCaptured, new MouseInteractionCapturedEventArgs { MonitorInfo = _monitorInfo, MouseEventInfo = mouseEventInfo });
            _mousePathFigure.Segments.Clear();
            _isDragging = false;
            eventArgs.Handled = true;
        }

        private void MousePositionView_KeyDown(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Key == Key.Escape)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.MouseInteractionCaptured, new MouseInteractionCapturedEventArgs());
                eventArgs.Handled = true;
            }
        }
    }
}
