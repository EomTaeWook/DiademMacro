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
    public class SequentialModeController : MacroModeControllerBase
    {
        private readonly InputEventExecutor _eventProcessorHandler;
        private CacheDataManager _cacheDataManager;
        public SequentialModeController(Config config,
            InputEventExecutor inputEventProcessor,
            CacheDataManager cacheDataManager, ScreenCaptureManager screenCaptureManager) : base(config, screenCaptureManager)
        {
            _eventProcessorHandler = inputEventProcessor;
            _cacheDataManager = cacheDataManager;
        }

        public override void Execute(
            ArrayQueue<Process> processes,
            ArrayQueue<EventInfoModel> eventInfoModels,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < processes.Count; ++i)
            {
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return;
                }
                var process = processes[i];

                ProcessEventInfos(process, eventInfoModels, cancellationToken);

                if (TaskHelper.TokenCheckDelay(_config.ProcessPeriod, cancellationToken) == false)
                {
                    return;
                }
            }
        }
        private EventResult HandleEvent(
            Bitmap capturedImage,
            Process process,
            EventInfoModel eventInfoModel,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return new EventResult(false, null);
            }
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

            var matchResult = CalculateSimilarityAndLocation(eventInfoModel.Image, capturedImage, eventInfoModel, cancellationToken);

            var similarity = matchResult.Item1;
            Point2D matchedLocation = matchResult.Item2;

            Draw(capturedImage);

            LogHelper.Debug($"Similarity : {matchResult.Item1} % max Loc : X : {matchedLocation.X} Y: {matchedLocation.Y}");

            if (similarity < GetSimilarityThreshold(eventInfoModel))
            {
                ReportStatus(eventInfoModel, $"Failed ({similarity}%)");
                TaskHelper.TokenCheckDelay(_config.ItemDelay, cancellationToken);
                return new EventResult(false, null);
            }
            if (eventInfoModel.SubEventItems.Count > 0)
            {
                ProcessSubEventInfos(process, eventInfoModel, cancellationToken);
            }
            else if (eventInfoModel.SameImageDrag == true)
            {
                for (int i = 0; i < eventInfoModel.MaxDragCount; ++i)
                {
                    if (cancellationToken.IsCancellationRequested == true)
                    {
                        return new EventResult(false, null);
                    }
                    var locations = OpenCVHelper.MultipleSearch(capturedImage, eventInfoModel.Image, GetSimilarityThreshold(eventInfoModel), 2, cancellationToken, _config.SearchImageResultDisplay);

                    if (locations.Count > 1)
                    {
                        var startPoint = new Point2D(locations[0].X + eventInfoModel.Image.Width / 2,
                            locations[0].Y + eventInfoModel.Image.Height / 2);

                        startPoint.X += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Width / 2);
                        startPoint.Y += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Height / 2);

                        var endPoint = new Point2D(locations[1].X + eventInfoModel.Image.Width / 2,
                            locations[1].Y + eventInfoModel.Image.Height / 2);

                        endPoint.X += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Width / 2);
                        endPoint.Y += _eventProcessorHandler.GetRandomValue(0, eventInfoModel.Image.Height / 2);

                        _eventProcessorHandler.ProcessSameImageMouseDragEvent(windowHandle, startPoint, endPoint, eventInfoModel, _config.DragDelay, cancellationToken);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return new EventResult(false, null);
                }
                if (eventInfoModel.EventType == EventType.Mouse)
                {
                    _eventProcessorHandler.ProcessMouseEvent(windowHandle, eventInfoModel, matchedLocation, template, _config.DragDelay, cancellationToken);
                }
                else if (eventInfoModel.EventType == EventType.Image)
                {
                    _eventProcessorHandler.ProcessImageEvent(windowHandle, eventInfoModel, matchedLocation, template, capturedImage.Size, cancellationToken);
                }
                else if (eventInfoModel.EventType == EventType.RelativeToImage)
                {
                    _eventProcessorHandler.ProcessRelativeToImageEvent(windowHandle, eventInfoModel, matchedLocation, template, capturedImage.Size, cancellationToken);
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

        private void ProcessSubEventInfos(Process process, EventInfoModel model, CancellationToken cancellationToken)
        {
            int repeatLimit = model.RepeatInfo.Count;
            if (model.RepeatInfo.RepeatType != RepeatType.Count)
            {
                repeatLimit = int.MaxValue;
            }
            for (int repeatIndex = 0; repeatIndex < repeatLimit; ++repeatIndex)
            {
                if (TaskHelper.TokenCheckDelay(model.AfterDelay, cancellationToken) == false)
                {
                    break;
                }

                if (_screenCaptureManager.CaptureProcessWindow(process, cancellationToken, out Bitmap capturedImage) == false)
                {
                    break;
                }

                using (capturedImage)
                {
                    bool childEventHandled = false;
                    bool shouldStopRepeating = false;
                    for (int childIndex = 0; childIndex < model.SubEventItems.Count; ++childIndex)
                    {
                        EventInfoModel childEventInfo = model.SubEventItems[childIndex];
                        if (childEventInfo.IsChecked == false)
                        {
                            continue;
                        }
                        childEventHandled = true;
                        EventResult childResult = HandleEvent(capturedImage, process, childEventInfo, cancellationToken);
                        if (model.RepeatInfo.RepeatType == RepeatType.RepeatOnChildEvent)
                        {
                            if (childResult.IsSuccess == false)
                            {
                                shouldStopRepeating = true;
                                break;
                            }
                        }
                        if (cancellationToken.IsCancellationRequested == true)
                        {
                            break;
                        }
                    }
                    if (model.RepeatInfo.RepeatType == RepeatType.RepeatOnChildEvent)
                    {
                        if (childEventHandled == false)
                        {
                            break;
                        }
                        if (shouldStopRepeating == true)
                        {
                            break;
                        }
                    }
                }

                if (cancellationToken.IsCancellationRequested == true)
                {
                    break;
                }
                if (model.RepeatInfo.RepeatType == RepeatType.StopOnParentImage)
                {
                    if (_screenCaptureManager.CaptureProcessWindow(process, cancellationToken, out Bitmap parentImage) == false)
                    {
                        break;
                    }
                    using (parentImage)
                    {
                        if (CalculateSimilarityAndLocation(model.Image, parentImage, model, cancellationToken).Item1 >= GetSimilarityThreshold(model))
                        {
                            break;
                        }
                    }
                }
            }
        }
        private void ProcessEventInfos(Process process, ArrayQueue<EventInfoModel> eventInfoModels, CancellationToken cancellationToken)
        {
            for (int eventIndex = 0; eventIndex < eventInfoModels.Count; ++eventIndex)
            {
                if (cancellationToken.IsCancellationRequested == true)
                {
                    return;
                }
                if (_screenCaptureManager.CaptureProcessWindow(process, cancellationToken, out Bitmap capturedImage) == false)
                {
                    TaskHelper.TokenCheckDelay(_config.ProcessPeriod, cancellationToken);
                    continue;
                }

                using (capturedImage)
                {
                    Draw(capturedImage);
                    EventInfoModel model = eventInfoModels[eventIndex];
                    EventResult result = HandleEvent(capturedImage, process, model, cancellationToken);
                    if (cancellationToken.IsCancellationRequested == true)
                    {
                        return;
                    }
                    EventInfoModel nextEventInfo = result.NextEventInfoModel;
                    if (nextEventInfo != null)
                    {
                        if (TryGetIndexByItemIndex(eventInfoModels, nextEventInfo.ItemIndex, out int nextIndex) == true)
                        {
                            eventIndex = nextIndex - 1;
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
