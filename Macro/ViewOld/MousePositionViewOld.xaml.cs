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
    public partial class MousePositionViewOld : Window
    {
        private readonly MonitorInfo _monitorInfo;
        private bool _isDrag;
        private readonly PathFigure _pathFigure;
        public MousePositionViewOld(MonitorInfo monitorInfo)
        {
            _pathFigure = new PathFigure
            {
                Segments = new PathSegmentCollection()
            };
            _monitorInfo = monitorInfo;
            InitializeComponent();
            Loaded += MousePositionView_Loaded;
        }
        public void ShowActivate()
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
            if (IsVisible && !_isDrag && e.LeftButton == MouseButtonState.Pressed)
                _isDrag = true;

            if (_isDrag && e.LeftButton == MouseButtonState.Pressed && IsVisible)
            {
                LineSegment segment = new LineSegment
                {
                    Point = e.GetPosition(this)
                };
                _pathFigure.Segments.Add(segment);
            }
            e.Handled = true;
        }
        private void MousePositionView_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (IsVisible && !_isDrag)
            {
                NotifyHelperOld.InvokeNotify(NotifyEventOldType.MousePointDataBind, new MousePointEventArgs()
                {
                    MouseTriggerInfo = new MouseTriggerInfo()
                    {
                        MouseInfoEventType = MouseEventType.RightClick,
                        StartPoint = PointToScreen(e.GetPosition(this))
                    },
                    MonitorInfo = _monitorInfo
                });
            }
            _isDrag = false;
        }

        private void MousePositionView_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            if (IsVisible && !_isDrag)
            {
                NotifyHelperOld.InvokeNotify(NotifyEventOldType.MousePointDataBind, new MousePointEventArgs()
                {
                    MouseTriggerInfo = new MouseTriggerInfo()
                    {
                        MouseInfoEventType = MouseEventType.LeftClick,
                        StartPoint = PointToScreen(e.GetPosition(this))
                    },
                    MonitorInfo = _monitorInfo
                });
            }
            else if (IsVisible && _isDrag)
            {
                NotifyHelperOld.InvokeNotify(NotifyEventOldType.MousePointDataBind, new MousePointEventArgs()
                {
                    MouseTriggerInfo = new MouseTriggerInfo()
                    {
                        MouseInfoEventType = MouseEventType.Drag,
                        StartPoint = PointToScreen(_pathFigure.StartPoint),
                        MiddlePoint = _pathFigure.Segments.Select(r => PointToScreen((r as LineSegment).Point)).ToList(),
                        EndPoint = PointToScreen(e.GetPosition(this)),
                    },
                    MonitorInfo = _monitorInfo
                });
            }
            _isDrag = false;
        }

        private void OnPreviewMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (IsVisible)
                _isDrag = false;
            _pathFigure.StartPoint = e.GetPosition(this);
            e.Handled = true;
        }

        private void MousePositionView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                NotifyHelperOld.InvokeNotify(NotifyEventOldType.MousePointDataBind, new MousePointEventArgs()
                {
                    MouseTriggerInfo = new MouseTriggerInfo()
                    {
                        MouseInfoEventType = MouseEventType.None,
                    },
                    MonitorInfo = _monitorInfo
                });
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }
        private void Clear()
        {
            _pathFigure.Segments.Clear();
        }
        private void Init()
        {
            pathMousePoint.Data = new PathGeometry()
            {
                Figures = new PathFigureCollection() { _pathFigure }
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
