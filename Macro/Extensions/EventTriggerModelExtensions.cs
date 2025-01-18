using Macro.Models;
using System.Collections.Generic;
using System.Linq;
using Utils.Infrastructure;
using Utils.Models;

namespace Macro.Extensions
{
    public static class EventTriggerModelExtensions
    {

        public static ValueConditionModel Clone(this ValueConditionModel source)
        {
            return new ValueConditionModel()
            {
                ConditionType = source.ConditionType,
                Value = source.Value
            };
        }
        public static MouseTriggerInfo Clone(this MouseTriggerInfo source)
        {
            return new MouseTriggerInfo()
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
        public static Rect Clone(this Rect source)
        {
            return new Rect()
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
                Rect = source.Rect.Clone()
            };
        }

        public static bool TryFindTriggerIndex(this IEnumerable<EventTriggerModel> eventTriggerModels, ulong triggerIndex, out int index)
        {
            index = -1;
            foreach (var item in eventTriggerModels)
            {
                index++;
                if (item.TriggerIndex == triggerIndex)
                {
                    return true;
                }
            }
            return false;
        }

    }
}
