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
    internal abstract class MacroModeControllerBase : IMacroModeController
    {
        protected Config _config;
        protected Action<Bitmap> _drawImageCallback;
        public MacroModeControllerBase(Config config)
        {
            _config = config;
        }
        public abstract void Execute(
            ArrayQueue<Process> processes,
            ArrayQueue<EventTriggerModel> eventTriggerModels,
            CancellationToken cancellationToken);

        public void SetDrawImageCallback(Action<Bitmap> drawImageCallback)
        {
            _drawImageCallback = drawImageCallback;
        }

        protected Tuple<int, Point2D> CalculateSimilarityAndLocation(Bitmap searchImage, Bitmap sourceBmp, EventTriggerModel eventTriggerModel)
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
    }
}
