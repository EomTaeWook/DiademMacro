using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using Utils.Infrastructure;

namespace Macro.Models.Converters
{
    public static class EventModelConverter
    {
        public static EventInfoModel ToEventInfoModel(EventTriggerModel sourceEventTriggerModel)
        {
            if (sourceEventTriggerModel == null)
            {
                return null;
            }

            var targetEventInfoModel = new EventInfoModel
            {
                AfterDelay = sourceEventTriggerModel.AfterDelay,
                EventToNext = sourceEventTriggerModel.EventToNext,
                EventType = sourceEventTriggerModel.EventType,
                HardClick = sourceEventTriggerModel.HardClick,
                Image = sourceEventTriggerModel.Image != null ? new Bitmap(sourceEventTriggerModel.Image) : null,
                IsChecked = sourceEventTriggerModel.IsChecked,
                ItemIndex = sourceEventTriggerModel.ItemIndex,
                KeyboardCmd = sourceEventTriggerModel.KeyboardCmd,
                MaxDragCount = sourceEventTriggerModel.MaxDragCount,
                MonitorInfo = sourceEventTriggerModel.MonitorInfo,
                MouseEventInfo = new MouseEventInfoV2(),
                ProcessInfo = sourceEventTriggerModel.ProcessInfo,
                RepeatInfo = sourceEventTriggerModel.RepeatInfo,
                RoiDataInfo = sourceEventTriggerModel.RoiDataInfo,
                SameImageDrag = sourceEventTriggerModel.SameImageDrag,
                SubEventItems = new ObservableCollection<EventInfoModel>()
            };

            if (targetEventInfoModel.EventType == Infrastructure.EventType.RelativeToImage)
            {
                if (sourceEventTriggerModel.MouseEventInfo != null)
                {
                    targetEventInfoModel.PositionRelativeToImage = new Point2D()
                    {
                        X = (int)sourceEventTriggerModel.MouseEventInfo.StartPoint.X,
                        Y = (int)sourceEventTriggerModel.MouseEventInfo.StartPoint.Y
                    };
                }
            }

            if (sourceEventTriggerModel.MouseEventInfo != null)
            {
                var soureMouseEventInfo = sourceEventTriggerModel.MouseEventInfo;

                targetEventInfoModel.MouseEventInfo.MouseInfoEventType = soureMouseEventInfo.MouseInfoEventType;

                if (soureMouseEventInfo.StartPoint != null)
                {
                    targetEventInfoModel.MouseEventInfo.MousePoint = new Utils.Infrastructure.Point2D()
                    {
                        X = (int)soureMouseEventInfo.StartPoint.X,
                        Y = (int)soureMouseEventInfo.StartPoint.Y,
                    };
                }

                if (soureMouseEventInfo.MiddlePoint?.Count > 0)
                {
                    targetEventInfoModel.MouseEventInfo.MousePoints.AddRange(soureMouseEventInfo.MiddlePoint.Select(r => new Point2D(r.X, r.Y)));

                    if (soureMouseEventInfo.EndPoint != null)
                    {
                        targetEventInfoModel
                            .MouseEventInfo
                            .MousePoints.Add(new Utils.Infrastructure.Point2D()
                            {
                                X = (int)soureMouseEventInfo.EndPoint.X,
                                Y = (int)soureMouseEventInfo.EndPoint.Y,
                            });
                    }
                }
            }

            if (sourceEventTriggerModel.SubEventItems != null)
            {
                foreach (var childEventTriggerModel in sourceEventTriggerModel.SubEventItems)
                {
                    var childEventInfoModel = ToEventInfoModel(childEventTriggerModel);
                    if (childEventInfoModel != null)
                    {
                        targetEventInfoModel.SubEventItems.Add(childEventInfoModel);
                    }
                }
            }

            return targetEventInfoModel;
        }
        public static List<EventInfoModel> ToEventInfoList(IEnumerable<EventTriggerModel> sourceEventTriggerModels)
        {
            var resultEventInfoModels = new List<EventInfoModel>();
            if (sourceEventTriggerModels == null)
            {
                return resultEventInfoModels;
            }

            foreach (var sourceEventTriggerModel in sourceEventTriggerModels)
            {
                var convertedEventInfoModel = ToEventInfoModel(sourceEventTriggerModel);
                if (convertedEventInfoModel != null)
                {
                    resultEventInfoModels.Add(convertedEventInfoModel);
                }
            }

            return resultEventInfoModels;
        }
    }
}
