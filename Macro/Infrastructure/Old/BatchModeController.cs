using DataContainer.Generated;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Extensions;
using Macro.Infrastructure.Manager;
using Macro.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using Utils;
using Utils.Infrastructure;

namespace Macro.Infrastructure.ControllerOld
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    internal class BatchModeController : MacroModeControllerBase
    {
        private readonly InputEventProcessorHandler _eventProcessorHandler;
        private CacheDataManager _cacheDataManager;
        public BatchModeController(Config config,
            InputEventProcessorHandler inputEventProcessorHandler,
            CacheDataManager cacheDataManager) : base(config)
        {
            _eventProcessorHandler = inputEventProcessorHandler;
            _cacheDataManager = cacheDataManager;
        }


        public override void Execute(
            ArrayQueue<Process> processes,
            ArrayQueue<EventTriggerModel> eventTriggerModels,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < processes.Count; ++i)
            {
                var process = processes[i];

                ProcessEventTriggers(process, eventTriggerModels, cancellationToken);

                TaskHelper.TokenCheckDelay(_config.ProcessPeriod, cancellationToken);
            }
        }
        private void ProcessSubEventTriggers(
            Process process,
            EventTriggerModel model,
            CancellationToken cancellationToken)
        {
            for (int i = 0; i < model.RepeatInfo.Count; ++i)
            {
                if (TaskHelper.TokenCheckDelay(model.AfterDelay, cancellationToken) == false)
                {
                    break;
                }

                if (_screenCaptureManager.CaptureProcessWindow(process,
                    out Bitmap sourceBmp) == false)
                {
                    break;
                }

                for (int ii = 0; ii < model.SubEventItems.Count; ++ii)
                {
                    var childResult = HandleEvent(
                        sourceBmp,
                        process,
                        model.SubEventItems[ii],
                        cancellationToken);
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

                if (model.RepeatInfo.RepeatType == RepeatType.StopOnParentImage)
                {
                    if (_screenCaptureManager.CaptureProcessWindow(process, out sourceBmp) == false)
                    {
                        break;
                    }

                    if (CalculateSimilarityAndLocation(model.Image, sourceBmp, model).Item1 >= _config.Similarity)
                    {
                        break;
                    }
                }
            }
        }
        private EventResult HandleEvent(
            Bitmap capturedImage,
            Process process,
            EventTriggerModel eventTriggerModel,
            CancellationToken cancellationToken)
        {
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

            var copyBitmap = (Bitmap)capturedImage.Clone();

            var matchResult = CalculateSimilarityAndLocation(eventTriggerModel.Image, copyBitmap, eventTriggerModel);

            var similarity = matchResult.Item1;
            Point2D matchedLocation = matchResult.Item2;

            _drawImageCallback?.Invoke(copyBitmap);

            LogHelper.Debug($"Similarity : {matchResult.Item1} % max Loc : X : {matchedLocation.X} Y: {matchedLocation.Y}");

            if (similarity < _config.Similarity)
            {
                TaskHelper.TokenCheckDelay(_config.ItemDelay, cancellationToken);
                return new EventResult(false, null);
            }

            if (eventTriggerModel.SubEventItems.Count > 0)
            {
                ProcessSubEventTriggers(process, eventTriggerModel, cancellationToken);
            }
            else if (eventTriggerModel.SameImageDrag == true)
            {
                for (int i = 0; i < eventTriggerModel.MaxDragCount; ++i)
                {
                    var locations = OpenCVHelper.MultipleSearch(capturedImage, eventTriggerModel.Image, _config.Similarity, 2, _config.SearchImageResultDisplay);

                    if (locations.Count > 1)
                    {
                        var startPoint = new Point2D(locations[0].X + eventTriggerModel.Image.Width / 2,
                            locations[0].Y + eventTriggerModel.Image.Height / 2);

                        startPoint.X += _eventProcessorHandler.GetRandomValue(0, eventTriggerModel.Image.Width / 2);
                        startPoint.Y += _eventProcessorHandler.GetRandomValue(0, eventTriggerModel.Image.Height / 2);

                        var endPoint = new Point2D(locations[1].X + eventTriggerModel.Image.Width / 2,
                            locations[1].Y + eventTriggerModel.Image.Width / 2);

                        endPoint.X += _eventProcessorHandler.GetRandomValue(0, eventTriggerModel.Image.Width / 2);
                        endPoint.Y += _eventProcessorHandler.GetRandomValue(0, eventTriggerModel.Image.Height / 2);

                        _eventProcessorHandler.SameImageMouseDragTriggerProcess(windowHandle, startPoint, endPoint, eventTriggerModel, _config.DragDelay);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                if (eventTriggerModel.EventType == EventType.Mouse)
                {
                    _eventProcessorHandler.HandleMouseEvent(windowHandle,
                        eventTriggerModel,
                        matchedLocation,
                        template,
                        _config.DragDelay);
                }
                else if (eventTriggerModel.EventType == EventType.Image)
                {
                    _eventProcessorHandler.HandleImageEvent(windowHandle,
                        eventTriggerModel,
                        matchedLocation,
                        template);
                }
                else if (eventTriggerModel.EventType == EventType.RelativeToImage)
                {
                    _eventProcessorHandler.HandleRelativeToImageEvent(windowHandle,
                        eventTriggerModel,
                        matchedLocation,
                        template);
                }
                else if (eventTriggerModel.EventType == EventType.Keyboard)
                {
                    _eventProcessorHandler.KeyboardTriggerProcess(windowHandle, eventTriggerModel);
                }

                EventTriggerModel nextModel = null;

                if (eventTriggerModel.EventToNext > 0 && eventTriggerModel.TriggerIndex != eventTriggerModel.EventToNext)
                {
                    nextModel = _cacheDataManager.GetEventTriggerModel(eventTriggerModel.EventToNext);

                    if (nextModel != null)
                    {
                        LogHelper.Debug($">>>>Next Move Event : CurrentIndex [ {eventTriggerModel.TriggerIndex} ] NextIndex [ {nextModel.TriggerIndex} ] ");
                    }
                }
                TaskHelper.TokenCheckDelay(eventTriggerModel.AfterDelay, cancellationToken);

                return new EventResult(true, nextModel);
            }

            return new EventResult(false, null);
        }

        private void ProcessEventTriggers(
            Process process,
            ArrayQueue<EventTriggerModel> eventTriggerModels,
            CancellationToken cancellationToken)
        {
            if (_screenCaptureManager.CaptureProcessWindow(process,
                    out Bitmap sourceBmp) == false)
            {
                return;
            }

            _drawImageCallback?.Invoke(sourceBmp);

            for (int i = 0; i < eventTriggerModels.Count; ++i)
            {
                var model = eventTriggerModels[i];
                var result = HandleEvent(sourceBmp, process, model, cancellationToken);

                var nextEventTrigger = result.NextEventTrigger;
                if (nextEventTrigger != null)
                {
                    if (eventTriggerModels.TryFindTriggerIndex(nextEventTrigger.TriggerIndex, out int index) == true)
                    {
                        i = index - 1;
                    }
                }
            }
        }
    }
}
