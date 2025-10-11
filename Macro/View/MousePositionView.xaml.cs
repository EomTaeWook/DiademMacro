using Macro.Infrastructure;
using Macro.Models;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Utils.Infrastructure;

namespace Macro.View
{
    /// <summary>
    /// MousePositionView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MousePositionView : Window
    {
        private readonly MonitorInfo _monitorInfo;
        private bool _isDragging;
        private readonly PathFigure _mousePathFigure;
        public MousePositionView(MonitorInfo monitorInfo)
        {
            _mousePathFigure = new PathFigure
            {
                Segments = new PathSegmentCollection()
            };
            _monitorInfo = monitorInfo;
            InitializeComponent();
            Loaded += MousePositionView_Loaded;
        }
        public void ShowAndActivate()
        {
            Clear();
            Show();
            Activate();
        }

        private void MousePositionView_Loaded(object sender, RoutedEventArgs e)
        {
            EventInit();
            Init();
            Activate();
        }
        private void EventInit()
        {
            PreviewKeyDown += MousePositionView_PreviewKeyDown;
            PreviewMouseLeftButtonDown += OnPreviewMouseButtonDown;
            PreviewMouseRightButtonDown += OnPreviewMouseButtonDown;

            PreviewMouseMove += MousePositionView_PreviewMouseMove;

            PreviewMouseLeftButtonUp += MousePositionView_PreviewMouseLeftButtonUp;
            PreviewMouseRightButtonUp += MousePositionView_PreviewMouseRightButtonUp;
        }

        private void MousePositionView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (IsVisible == false)
            {
                return;
            }

            if (!_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                _isDragging = true;
            }

            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                LineSegment segment = new LineSegment
                {
                    Point = e.GetPosition(this)
                };
                _mousePathFigure.Segments.Add(segment);
            }
            e.Handled = true;
        }
        private void MousePositionView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsVisible == false)
            {
                return;
            }
            e.Handled = true;

            if (!_isDragging)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.MouseInteractionCaptured, new MouseInteractionCapturedEventArgs()
                {
                    MouseEventInfo = new MouseEventInfoV2()
                    {
                        MouseEventType = MouseEventType.RightClick,
                        MousePoint = ConvertToPoint2D(PointToScreen(e.GetPosition(this)))
                    },
                    MonitorInfo = _monitorInfo
                });
            }
            _mousePathFigure.Segments.Clear();
            _isDragging = false;
        }
        private Point2D ConvertToPoint2D(Point point)
        {
            return new Point2D(point.X, point.Y);
        }
        private void MousePositionView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (IsVisible == false)
            {
                return;
            }
            e.Handled = true;
            var endPointScreen = PointToScreen(e.GetPosition(this));

            if (!_isDragging)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.MouseInteractionCaptured, new MouseInteractionCapturedEventArgs
                {
                    MouseEventInfo = new MouseEventInfoV2
                    {
                        MouseEventType = MouseEventType.LeftClick,
                        MousePoint = ConvertToPoint2D(endPointScreen)
                    },
                    MonitorInfo = _monitorInfo
                });
            }
            else if (_isDragging)
            {
                var pathPoints = _mousePathFigure.Segments.
                    OfType<LineSegment>()
                    .Select(r => ConvertToPoint2D(PointToScreen(r.Point)))
                    .ToList();

                pathPoints.Add(ConvertToPoint2D(endPointScreen));

                NotifyHelper.InvokeNotify(NotifyEventType.MouseInteractionCaptured, new MouseInteractionCapturedEventArgs()
                {
                    MouseEventInfo = new MouseEventInfoV2()
                    {
                        MouseEventType = MouseEventType.Drag,
                        MousePoint = ConvertToPoint2D(PointToScreen(_mousePathFigure.StartPoint)),
                        MousePoints = pathPoints
                    },
                    MonitorInfo = _monitorInfo
                });
            }
            _mousePathFigure.Segments.Clear();
            _isDragging = false;
        }

        private void OnPreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsVisible == false)
            {
                return;
            }
            _isDragging = false;
            _mousePathFigure.Segments.Clear();
            _mousePathFigure.StartPoint = e.GetPosition(this);
            e.Handled = true;
        }

        private void MousePositionView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                NotifyHelper.InvokeNotify(NotifyEventType.MouseInteractionCaptured, new MouseInteractionCapturedEventArgs());
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }
        private void Clear()
        {
            _mousePathFigure.Segments.Clear();
        }
        private void Init()
        {
            pathMousePoint.Data = new PathGeometry()
            {
                Figures = new PathFigureCollection() { _mousePathFigure }
            };
#if !DEBUG
            Topmost = true;
#endif
            Left = _monitorInfo.Rect.Left;
            Width = _monitorInfo.Rect.Width;
            Top = _monitorInfo.Rect.Top;
            Height = _monitorInfo.Rect.Height;
            WindowState = WindowState.Maximized;
        }
    }
}
