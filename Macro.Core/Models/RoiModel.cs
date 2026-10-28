using System;
using Utils.Infrastructure;

namespace Macro.Models
{
    [Serializable]
    public class RoiModel
    {
        public Rectangle RoiRect { get; set; } = null;
        public MonitorInfo MonitorInfo { get; set; } = null;

        public bool IsExists()
        {
            return RoiRect != null && MonitorInfo != null;
        }
    }
}
