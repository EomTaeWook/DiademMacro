using Macro.Infrastructure;
using System;
using System.Collections.Generic;
using System.Windows;
using Utils.Infrastructure;

namespace Macro.Models
{
    [Obsolete]
    [Serializable]
    public class MouseEventInfo
    {
        public MouseEventType MouseInfoEventType { get; set; } = MouseEventType.None;

        public Point StartPoint { get; set; } = new Point();

        public List<Point> MiddlePoint { get; set; } = new List<Point>();

        public Point EndPoint { get; set; } = new Point();

        public short WheelData { get; set; }
    }

    public class MouseEventInfoV2
    {
        public MouseEventType MouseEventType { get; set; } = MouseEventType.None;
        public Point2D MousePoint { get; set; } = new Point2D();
        public List<Point2D> MousePoints { get; set; } = new List<Point2D>();
    }
}
