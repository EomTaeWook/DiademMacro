using Dignus.Collections;
using Dignus.Log;
using Macro.Infrastructure.Interfaces;
using Macro.Infrastructure.Manager;
using Macro.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Controller
{
    public abstract class MacroModeControllerBase : IMacroModeController
    {
        protected Config _config;
        private Action<Bitmap> _drawImageCallback;
        private Action<EventInfoModel, string> _statusCallback;
        protected ScreenCaptureManager _screenCaptureManager;
        public MacroModeControllerBase(Config config, ScreenCaptureManager screenCaptureManager)
        {
            _config = config;
            _screenCaptureManager = screenCaptureManager;
        }
        public abstract void Execute(
            ArrayQueue<Process> processes,
            ArrayQueue<EventInfoModel> eventInfoModels,
            CancellationToken cancellationToken);

        public void SetDrawImageCallback(Action<Bitmap> drawImageCallback)
        {
            _drawImageCallback = drawImageCallback;
        }

        public void SetStatusCallback(Action<EventInfoModel, string> statusCallback)
        {
            _statusCallback = statusCallback;
        }

        protected void ReportStatus(EventInfoModel model, string status)
        {
            _statusCallback?.Invoke(model, status);
        }

        protected int GetSimilarityThreshold(EventInfoModel model)
        {
            if (model.Similarity > 0)
            {
                return model.Similarity;
            }
            return _config.Similarity;
        }

        public void Draw(Bitmap bitmap)
        {
            _drawImageCallback?.Invoke(bitmap);
        }

        protected Tuple<int, Point2D> CalculateSimilarityAndLocation(Bitmap searchImage, Bitmap sourceBmp, EventInfoModel eventInfoModel)
        {
            var similarity = 0;
            Point2D matchedLocation = new Point2D(0, 0);

            if (eventInfoModel.RoiDataInfo.IsExists() == true)
            {
                var newRect = _screenCaptureManager.AdjustRectForDPI(eventInfoModel.RoiDataInfo.RoiRect, eventInfoModel.RoiDataInfo.MonitorInfo);

                int imageWidth = sourceBmp.Width;
                int imageHeight = sourceBmp.Height;

                bool regionOutsideImage = false;
                if (newRect.Left < 0)
                {
                    regionOutsideImage = true;
                }
                if (newRect.Right > imageWidth)
                {
                    regionOutsideImage = true;
                }
                if (newRect.Top < 0)
                {
                    regionOutsideImage = true;
                }
                if (newRect.Bottom > imageHeight)
                {
                    regionOutsideImage = true;
                }
                if (regionOutsideImage == true)
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

                Bitmap regionImage = null;
                try
                {
                    regionImage = OpenCVHelper.CropImage(sourceBmp, newRect);
                }
                catch (Exception exception)
                {
                    LogHelper.Error(exception);
                }

                if (regionImage != null)
                {
                    try
                    {
                        similarity = OpenCVHelper.Search(regionImage, searchImage, out matchedLocation, _config.SearchImageResultDisplay);
                        matchedLocation.X += newRect.Left;
                        matchedLocation.Y += newRect.Top;
                    }
                    finally
                    {
                        if (ReferenceEquals(regionImage, sourceBmp) == false)
                        {
                            regionImage.Dispose();
                        }
                    }
                }
            }
            else
            {
                similarity = OpenCVHelper.Search(sourceBmp, searchImage, out matchedLocation, _config.SearchImageResultDisplay);
            }
            return Tuple.Create(similarity, matchedLocation);
        }
    }
}
