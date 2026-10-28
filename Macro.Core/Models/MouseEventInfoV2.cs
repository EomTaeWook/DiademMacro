using Macro.Infrastructure;
using System.Collections.Generic;
using Utils.Infrastructure;

namespace Macro.Models
{
    public class MouseEventInfoV2
    {
        public MouseEventType MouseEventType { get; set; } = MouseEventType.None;
        public Point2D MousePoint { get; set; } = new();
        public List<Point2D> MousePoints { get; set; } = new();
    }
}
