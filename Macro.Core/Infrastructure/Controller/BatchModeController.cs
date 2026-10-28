using DataContainer.Generated;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Infrastructure.Manager;
using Macro.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using Utils;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Controller
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class BatchModeController : MacroModeControllerBase
    {
        private readonly InputEventExecutor _eventProcessorHandler;
        private readonly CacheDataManager _cacheDataManager;
        public BatchModeController(Config config,
            InputEventExecutor inputEventProcessorHandler,
            CacheDataManager cacheDataManager, ScreenCaptureManager screenCaptureManager) : base(config, screenCaptureManager)
        {
            _eventProcessorHandler = inputEventProcessorHandler;
            _cacheDataManager = cacheDataManager;
        }


        public override void Execute(
            ArrayQueue<Process> processes,
            ArrayQueue<EventInfoModel> eventInfoModels,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < processes.Count; ++i)
            {
                var process = processes[i];

                ProcessEventInfos(process, eventInfoModels, cancellationToken);

                TaskHelper.TokenCheckDelay(_config.ProcessPeriod, cancellationToken);
            }
        }
        private void ProcessSubEventTriggers(Process process, EventInfoModel model, CancellationToken cancellationToken)
        {
            for (int repeatIndex = 0; repeatIndex < model.RepeatInfo.Count; ++repeatIndex)
            {
                if (TaskHelper.TokenCheckDelay(model.AfterDelay, cancellationToken) == false)
                {
                    break;
                }

                if (_screenCaptureManager.CaptureProcessWindow(process, out Bitmap capturedImage) == false)
                {
                    break;
                }

                using (capturedImage)
                {
                    for (int childIndex = 0; childIndex < model.SubEventItems.Count; ++childIndex)
                    {
                        EventResult childResult = HandleEvent(capturedImage, process, model.SubEventItems[childIndex], cancellationToken);
                        if (model.RepeatInfo.RepeatType == RepeatType.RepeatOnChildEvent)
                        {
                            if (childResult.IsSuccess == false)
                            {
                                break;
                            }
                        }
                        if (cancellationToken.IsCancellationRequested == true)
                        {
                            break;
                        }
                    }
                }

                if (model.RepeatInfo.RepeatType == RepeatType.StopOnParentImage)
                {
                    if (_screenCaptureManager.CaptureProcessWindow(process, out Bitmap parentImage) == false)
                    {
                        break;
                    }
                    using (parentImage)
                    {
                        if (CalculateSimilarityAndLocation(model.Image, parentImage, model).Item1 >= GetSimilarityThreshold(model))
                        {
                            break;
                        }
                    }
                }
            }
        }
        private EventResult HandleEvent(
            Bitmap capturedImage,
            Process process,
            EventInfoModel eventInfoModel,
            CancellationToken cancellationToken)
        {
            ReportStatus(eventInfoModel, "Searching");
            var windowHandle = IntPtr.Zero;
            var template = TemplateContainer<ApplicationTemplate>.Find(process.ProcessName);

            if (string.IsNullOrEmpty(template.HandleName))
            {
                windowHandle = process.MainWindowHandle;
            }
            else
            {
                var item = NativeHelper.GetChildHandles(process.MainWindowHandle).Where(r => r.Item1.Equals(template.HandleName)).FirstOrDefault();

                windowHandle = item != null ? item.Item2 : process.MainWindowHandle;
            }

            using Bitmap copyBitmap = (Bitmap)capturedImage.Clone();

            var matchResult = CalculateSimilarityAndLocation(eventInfoModel.Image, copyBitmap, eventInfoModel);

            var similarity = matchResult.Item1;
            Point2D matchedLocation = matchResult.Item2;

            Draw(copyBitmap);

            LogHelper.Debug($"Similarity : {matchResult.Item1} % max Loc : X : {matchedLocation.X} Y: {matchedLocation.Y}");

            if (similarity < GetSimilarityThreshold(eventInfoModel))
            {
                ReportStatus(eventInfoModel, $"Failed ({similarity}%)");
                TaskHelper.TokenCheckDelay(_config.ItemDelay, cancellationToken);
                return new EventResult(false, null);
            }

            if (eventInfoModel.SubEventItems.Count > 0)
            {
                ProcessSubEventTriggers(process, eventInfoModel, cancellationToken);
            }
            else if (eventInfoModel.SameImageDrag == true)
            {
                for (int i = 0; i < eventInfoModel.MaxDragCount; ++i)
                {
                    var locations = OpenCVHelper.MultipleSearch(capturedImage, eventInfoModel.Image, GetSimilarityThreshold(eventInfoModel), 2, _config.SearchImageResultDisplay);

                    if (locations.Count > 1)
                    {
                        var startPoint = new Point2D(locations[0].X + eventInfoModel.Image.Width / 2,
                            locations[0].Y + eventInfoModel.Image.Height / 2);

                        startPoint.X += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Width / 2);
                        startPoint.Y += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Height / 2);

                        var endPoint = new Point2D(locations[1].X + eventInfoModel.Image.Width / 2,
                            locations[1].Y + eventInfoModel.Image.Width / 2);

                        endPoint.X += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Width / 2);
                        endPoint.Y += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Height / 2);

                        _eventProcessorHandler.ProcessSameImageMouseDragEvent(windowHandle, startPoint, endPoint, eventInfoModel, _config.DragDelay);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                if (eventInfoModel.EventType == EventType.Mouse)
                {
                    _eventProcessorHandler.ProcessMouseEvent(windowHandle,
                        eventInfoModel,
                        matchedLocation,
                        template,
                        _config.DragDelay);
                }
                else if (eventInfoModel.EventType == EventType.Image)
                {
                    _eventProcessorHandler.ProcessImageEvent(windowHandle, eventInfoModel, matchedLocation, template, capturedImage.Size);
                }
                else if (eventInfoModel.EventType == EventType.RelativeToImage)
                {
                    _eventProcessorHandler.ProcessRelativeToImageEvent(windowHandle, eventInfoModel, matchedLocation, template, capturedImage.Size);
                }
                else if (eventInfoModel.EventType == EventType.Keyboard)
                {
                    _eventProcessorHandler.ProcessKeyboardEvent(windowHandle, eventInfoModel);
                }

                EventInfoModel nextModel = null;

                if (eventInfoModel.EventToNext > 0 && eventInfoModel.ItemIndex != eventInfoModel.EventToNext)
                {
                    nextModel = _cacheDataManager.GetEventInfoModel(eventInfoModel.EventToNext);

                    if (nextModel != null)
                    {
                        LogHelper.Debug($">>>>Next Move Event : CurrentIndex [ {eventInfoModel.ItemIndex} ] NextIndex [ {nextModel.ItemIndex} ] ");
                    }
                }
                TaskHelper.TokenCheckDelay(eventInfoModel.AfterDelay, cancellationToken);

                ReportStatus(eventInfoModel, $"Success ({similarity}%)");
                return new EventResult(true, nextModel);
            }

            ReportStatus(eventInfoModel, $"Success ({similarity}%)");
            return new EventResult(false, null);
        }

        private void ProcessEventInfos(Process process, ArrayQueue<EventInfoModel> eventInfoModels, CancellationToken cancellationToken)
        {
            if (_screenCaptureManager.CaptureProcessWindow(process, out Bitmap capturedImage) == false)
            {
                return;
            }

            using (capturedImage)
            {
                Draw(capturedImage);
                for (int eventIndex = 0; eventIndex < eventInfoModels.Count; ++eventIndex)
                {
                    EventInfoModel model = eventInfoModels[eventIndex];
                    EventResult result = HandleEvent(capturedImage, process, model, cancellationToken);
                    EventInfoModel nextEventInfo = result.NextEventInfoModel;
                    if (nextEventInfo != null)
                    {
                        if (TryGetIndexByItemIndex(eventInfoModels, nextEventInfo.ItemIndex, out int sourceIndex) == true)
                        {
                            eventIndex = sourceIndex - 1;
                        }
                    }
                }
            }
        }

        private bool TryGetIndexByItemIndex(ArrayQueue<EventInfoModel> source, ulong findItemIndex, out int sourceIndex)
        {
            sourceIndex = -1;

            foreach (var item in source)
            {
                sourceIndex++;
                if (item.ItemIndex == findItemIndex)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
