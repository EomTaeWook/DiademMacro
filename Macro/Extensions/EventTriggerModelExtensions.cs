using Macro.Models;
using System.Collections.Generic;
using System.Linq;
using Utils.Infrastructure;

namespace Macro.Extensions
{

    public static class EventInfoModelExtensions
    {
        public static MouseEventInfoV2 Clone(this MouseEventInfoV2 source)
        {
            var cloned = new MouseEventInfoV2
            {
                MouseEventType = source.MouseEventType,
                MousePoints = new List<Point2D>(source.MousePoints),
                MousePoint = new Point2D()
                {
                    X = source.MousePoint.X,
                    Y = source.MousePoint.Y,
                },

            };

            return cloned;
        }
        public static RoiModel Clone(this RoiModel source)
        {
            var cloned = new RoiModel();

            if (source.IsExists() == false)
            {
                return cloned;
            }
            cloned.MonitorInfo = source.MonitorInfo.Clone();
            cloned.RoiRect = new Rectangle()
            {
                Bottom = source.RoiRect.Bottom,
                Left = source.RoiRect.Left,
                Right = source.RoiRect.Right,
                Top = source.RoiRect.Top
            };
            return cloned;
        }
    }


    public static class EventTriggerModelExtensions
    {

        public static MouseEventInfo Clone(this MouseEventInfo source)
        {
            return new MouseEventInfo()
            {
                EndPoint = new System.Windows.Point()
                {
                    X = source.EndPoint.X,
                    Y = source.EndPoint.Y
                },
                MiddlePoint = source.MiddlePoint.Select(r => new System.Windows.Point()
                {
                    X = r.X,
                    Y = r.Y
                }).ToList(),
                MouseInfoEventType = source.MouseInfoEventType,
                StartPoint = new System.Windows.Point()
                {
                    X = source.StartPoint.X,
                    Y = source.StartPoint.Y
                },
                WheelData = source.WheelData
            };
        }
        public static IntRect Clone(this IntRect source)
        {
            return new IntRect()
            {
                Bottom = source.Bottom,
                Left = source.Left,
                Right = source.Right,
                Top = source.Top
            };
        }
        public static ProcessInfo Clone(this ProcessInfo source)
        {
            return new ProcessInfo()
            {
                Position = source.Position.Clone(),
                ProcessName = source.ProcessName.Clone() as string
            };
        }
        public static RepeatInfoModel Clone(this RepeatInfoModel source)
        {
            return new RepeatInfoModel()
            {
                Count = source.Count,
                RepeatType = source.RepeatType,
            };
        }
        public static MonitorInfo Clone(this MonitorInfo source)
        {
            return new MonitorInfo()
            {
                Dpi = new System.Drawing.Point()
                {
                    X = source.Dpi.X,
                    Y = source.Dpi.Y
                },
                Index = source.Index,
                Rect = source.Rect.Clone(),
                DeviceName = source.DeviceName,
                FriendlyName = source.FriendlyName,
                IsOn = source.IsOn
            };
        }

        public static bool TryFindTriggerIndex(this IEnumerable<EventTriggerModel> eventTriggerModels, ulong triggerIndex, out int index)
        {
            index = -1;
            foreach (var item in eventTriggerModels)
            {
                index++;
                if (item.ItemIndex == triggerIndex)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
