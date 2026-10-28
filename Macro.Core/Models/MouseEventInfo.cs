using Macro.Infrastructure;
using System;
using System.Collections.Generic;

namespace Macro.Models
{
    [Obsolete]
    [Serializable]
    public class MouseEventInfo
    {
        public MouseEventType MouseInfoEventType { get; set; } = MouseEventType.None;

        public PointModel StartPoint { get; set; } = new PointModel();

        public List<PointModel> MiddlePoint { get; set; } = new List<PointModel>();

        public PointModel EndPoint { get; set; } = new PointModel();

        public short WheelData { get; set; }
    }

}
