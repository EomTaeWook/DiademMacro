using System;
using System.Drawing;

namespace Utils.Infrastructure
{
    [Serializable]
    public class MonitorInfo
    {
        public IntRect Rect { get; set; }
        public int Index { get; set; }
        public Point Dpi { get; set; } = new Point();

        public string DeviceName { get; set; }

        public string FriendlyName { get; set; }

        public bool IsOn { get; set; }
    }
}
