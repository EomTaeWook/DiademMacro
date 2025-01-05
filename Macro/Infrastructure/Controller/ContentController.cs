using DataContainer.Generated;
using Dignus.Collections;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Extensions;
using Macro.Infrastructure.Manager;
using Macro.Models;
using Macro.View;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TemplateContainers;
using Utils;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Controller
{

    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class ContentController
    {
        private ContentView _contentView;
        private CancellationTokenSource _cts;
        private Process _fixProcess;
        private InputEventProcessorHandler _eventProcessorHandler;
        private Config _config;

        private CancellationToken _cancellationToken;
        public ContentController(Config config, InputEventProcessorHandler eventProcessorHandler)
        {
            _eventProcessorHandler = eventProcessorHandler;
            _config = config;
            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
        }


        private void NotifyHelper_ConfigChanged(ConfigEventArgs obj)
        {
            _config = obj.Config;
        }

        public void SetContentView(ContentView baseContentView)
        {
            this._contentView = baseContentView;
        }

        public bool Validate(EventTriggerModel model, out MessageTemplate messageTemplate)
        {
            messageTemplate = TemplateContainer<MessageTemplate>.Find(1000);

            model.KeyboardCmd = model.KeyboardCmd.Replace(" ", "");

            if (model.Image == null)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1001);
                return false;
            }
            if (model.EventType == EventType.Mouse && model.MouseTriggerInfo.MouseInfoEventType == MouseEventType.None)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1002);
                return false;
            }

            if (string.IsNullOrEmpty(model.KeyboardCmd) && model.EventType == EventType.Keyboard)
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1003);
                return false;
            }
            if (string.IsNullOrEmpty(model.ProcessInfo.ProcessName))
            {
                messageTemplate = TemplateContainer<MessageTemplate>.Find(1004);
                return false;
            }

            return true;
        }
        public void Start()
        {
            if (_cts == null)
            {
                _cts = new CancellationTokenSource();
                _cancellationToken = _cts.Token;
            }

            if (_config.MacroMode == MacroModeType.SequentialMode)
            {
                var _ = Task.Run(() => ProcessEventLoop());
            }
            else
            {
                var _ = Task.Run(() => ProcessEventLoop());
            }
        }

        private void ProcessEventLoop()
        {
            ArrayQueue<EventTriggerModel> models = new ArrayQueue<EventTriggerModel>();

            foreach (var item in _contentView.eventSettingView.GetDataContext().TriggerSaves)
            {
                if (item.IsChecked)
                {
                    models.Add(item);
                }
            }

            while (_cancellationToken.IsCancellationRequested == false)
            {
                if (_fixProcess != null)
                {
                    for (int i = 0; i < models.Count; ++i)
                    {
                        var result = HandleEvent(_fixProcess, models[i]);
                        var nextEventTrigger = result.NextEventTrigger;
                        if (nextEventTrigger != null)
                        {
                            if (models.TryFindTriggerIndex(nextEventTrigger.TriggerIndex, out int index) == true)
                            {
                                i = index - 1;
                            }
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < models.Count; ++i)
                    {
                        var model = models[i];
                        var processInfos = Process.GetProcessesByName(model.ProcessInfo.ProcessName);
                        foreach (var process in processInfos)
                        {
                            var result = HandleEvent(process, model);
                            var nextEventTrigger = result.NextEventTrigger;
                            if (nextEventTrigger != null)
                            {
                                if (models.TryFindTriggerIndex(nextEventTrigger.TriggerIndex, out int index) == true)
                                {
                                    i = index - 1;
                                    break;
                                }
                            }
                        }
                    }
                }
                TaskHelper.TokenCheckDelay(_config.ProcessPeriod, _cancellationToken);
            }
        }
        public void Stop()
        {
            if (_cts != null)
            {
                _cts.Cancel();
                _cts = null;
            }
        }
        public void SetFixProcess(Process process)
        {
            this._fixProcess = process;
        }


        private Tuple<int, Point2D> CalculateSimilarityAndLocation(Bitmap searchImage, Bitmap sourceBmp, EventTriggerModel eventTriggerModel)
        {
            var similarity = 0;
            Point2D matchedLocation = new Point2D(0, 0);

            if (eventTriggerModel.RoiData.IsExists() == true)
            {
                var newRect = DisplayHelper.ApplyMonitorDPI(eventTriggerModel.RoiData.RoiRect, eventTriggerModel.RoiData.MonitorInfo);

                int imageWidth = sourceBmp.Width;
                int imageHeight = sourceBmp.Height;

                if (newRect.Left < 0 || newRect.Right > imageWidth || newRect.Top < 0 || newRect.Bottom > imageHeight)
                {
                    newRect.Left = 0;
                    newRect.Right = imageWidth;
                    newRect.Top = 0;
                    newRect.Bottom = imageHeight;
                }
                else
                {
                    newRect.Left = Math.Max(0, Math.Min(newRect.Left, imageWidth - 1));
                    newRect.Right = Math.Max(newRect.Left + 1, Math.Min(newRect.Right, imageWidth - 1));
                    newRect.Top = Math.Max(0, Math.Min(newRect.Top, imageHeight - 1));
                    newRect.Bottom = Math.Max(newRect.Top + 1, Math.Min(newRect.Bottom, imageHeight - 1));

                }

                Bitmap roiBmp = null;
                try
                {
                    roiBmp = OpenCVHelper.CropImage(sourceBmp, newRect);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);
                }

                if (roiBmp != null)
                {
                    similarity = OpenCVHelper.Search(roiBmp, searchImage, out matchedLocation, _config.SearchImageResultDisplay);
                    matchedLocation.X += newRect.Left;
                    matchedLocation.Y += newRect.Top;
                }
            }
            else
            {
                similarity = OpenCVHelper.Search(sourceBmp, searchImage, out matchedLocation, _config.SearchImageResultDisplay);
            }

            return Tuple.Create(similarity, matchedLocation);
        }
        private void ProcessSubEventTriggers(Process process, EventTriggerModel model)
        {
            for (int i = 0; i < model.RepeatInfo.Count; ++i)
            {
                if (TaskHelper.TokenCheckDelay(model.AfterDelay, _cancellationToken) == false)
                {
                    break;
                }
                for (int ii = 0; ii < model.SubEventTriggers.Count; ++ii)
                {
                    var childResult = HandleEvent(process, model.SubEventTriggers[ii]);
                    if (model.RepeatInfo.RepeatType == RepeatType.NoSearchChild)
                    {
                        if (childResult.IsSuccess == false)
                        {
                            break;
                        }
                    }

                    if (_cancellationToken.IsCancellationRequested == true)
                    {
                        break;
                    }
                }

                if (model.RepeatInfo.RepeatType == RepeatType.SearchParent)
                {
                    if (DisplayHelper.ProcessCaptureV2(process, ApplicationManager.Instance.GetDrawWindowHandle(), out Bitmap sourceBmp) == false)
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
        private EventResult HandleEvent(Process process, EventTriggerModel model)
        {
            var hWnd = IntPtr.Zero;
            var template = TemplateContainer<ApplicationTemplate>.Find(model.ProcessInfo.ProcessName);

            if (string.IsNullOrEmpty(template.HandleName))
            {
                hWnd = process.MainWindowHandle;
            }
            else
            {
                var item = NativeHelper.GetChildHandles(process.MainWindowHandle).Where(r => r.Item1.Equals(template.HandleName)).FirstOrDefault();

                hWnd = item != null ? item.Item2 : process.MainWindowHandle;
            }

            if (DisplayHelper.ProcessCaptureV2(process, ApplicationManager.Instance.GetDrawWindowHandle(), out Bitmap sourceBmp) == false)
            {
                TaskHelper.TokenCheckDelay(_config.ItemDelay, _cancellationToken);
                return new EventResult(false, null);
            }

            var matchResult = CalculateSimilarityAndLocation(model.Image, sourceBmp, model);
            var similarity = matchResult.Item1;
            Point2D matchedLocation = matchResult.Item2;

            this._contentView.DrawCaptureImage(sourceBmp);
            LogHelper.Debug($"Similarity : {matchResult.Item1} % max Loc : X : {matchedLocation.X} Y: {matchedLocation.Y}");
            if (similarity < _config.Similarity)
            {
                TaskHelper.TokenCheckDelay(_config.ItemDelay, _cancellationToken);
                return new EventResult(false, null);
            }
            if (model.SubEventTriggers.Count > 0)
            {
                ProcessSubEventTriggers(process, model);
            }
            else if (model.SameImageDrag == true)
            {
                for (int i = 0; i < model.MaxSameImageCount; ++i)
                {
                    var locations = OpenCVHelper.MultipleSearch(sourceBmp, model.Image, _config.Similarity, 2, _config.SearchImageResultDisplay);

                    if (locations.Count > 1)
                    {
                        this._contentView.DrawCaptureImage(sourceBmp);
                        var startPoint = new Point2D(locations[0].X + model.Image.Width / 2, locations[0].Y + model.Image.Height / 2);

                        startPoint.X += _eventProcessorHandler.GetRandomValue(0, model.Image.Width / 2);
                        startPoint.Y += _eventProcessorHandler.GetRandomValue(0, model.Image.Height / 2);

                        var endPoint = new Point2D(locations[1].X + model.Image.Width / 2, locations[1].Y + model.Image.Width / 2);
                        endPoint.X += _eventProcessorHandler.GetRandomValue(0, model.Image.Width / 2);
                        endPoint.Y += _eventProcessorHandler.GetRandomValue(0, model.Image.Height / 2);

                        _eventProcessorHandler.SameImageMouseDragTriggerProcess(hWnd, startPoint, endPoint, model, _config.DragDelay);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                if (model.EventType == EventType.Mouse)
                {
                    _eventProcessorHandler.HandleMouseEvent(hWnd,
                        model,
                        matchedLocation,
                        template,
                        _config.DragDelay);
                }
                else if (model.EventType == EventType.Image)
                {
                    _eventProcessorHandler.HandleImageEvent(hWnd,
                        model,
                        matchedLocation,
                        template);
                }
                else if (model.EventType == EventType.RelativeToImage)
                {
                    _eventProcessorHandler.HandleRelativeToImageEvent(hWnd,
                        model,
                        matchedLocation,
                        template);
                }
                else if (model.EventType == EventType.Keyboard)
                {
                    _eventProcessorHandler.KeyboardTriggerProcess(hWnd, model);
                }

                EventTriggerModel nextModel = null;

                if (model.EventToNext > 0 && model.TriggerIndex != model.EventToNext)
                {
                    nextModel = CacheDataManager.Instance.GetEventTriggerModel(model.EventToNext);

                    if (nextModel != null)
                    {
                        LogHelper.Debug($">>>>Next Move Event : CurrentIndex [ {model.TriggerIndex} ] NextIndex [ {nextModel.TriggerIndex} ] ");
                    }
                }
                TaskHelper.TokenCheckDelay(model.AfterDelay, _cancellationToken);

                return new EventResult(true, nextModel);
            }

            return new EventResult(false, null);
        }


    }
}
