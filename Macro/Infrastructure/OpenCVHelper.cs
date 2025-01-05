using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using Utils.Infrastructure;
using Color = System.Drawing.Color;
using Point = OpenCvSharp.Point;
using Rect = Utils.Infrastructure.Rect;
using Rectangle = System.Drawing.Rectangle;

namespace Macro.Infrastructure
{
    public class OpenCVHelper
    {
        public static int Search(Bitmap source, Bitmap target, out Point2D location, bool isResultDisplay = false)
        {
            var sourceMat = BitmapConverter.ToMat(source);
            var targetMat = BitmapConverter.ToMat(target);
            if (sourceMat.Cols <= targetMat.Cols || sourceMat.Rows <= targetMat.Rows)
            {
                location = new Point2D();
                return 0;
            }

            var match = sourceMat.MatchTemplate(targetMat, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(match, out _, out double max, out _, out Point maxLoc);

            location = new Point2D()
            {
                X = maxLoc.X,
                Y = maxLoc.Y
            };
            if (isResultDisplay)
            {
                using (var g = Graphics.FromImage(source))
                {
                    using (var pen = new Pen(Color.Red, 2))
                    {
                        g.DrawRectangle(pen, new Rectangle() { X = (int)location.X, Y = (int)location.Y, Width = target.Width, Height = target.Height });
                    }
                }
            }
            return Convert.ToInt32(max * 100);
        }
        public static List<Point> MultipleSearch(Bitmap source, Bitmap target, int similarity, int maxSameRepeatCount, bool isResultDisplay = false)
        {
            var searchAndCopyImage = source.Clone() as Bitmap;

            var sourceMat = BitmapConverter.ToMat(searchAndCopyImage);
            var targetMat = BitmapConverter.ToMat(target);

            if (sourceMat.Cols <= targetMat.Cols || sourceMat.Rows <= targetMat.Rows)
            {
                return new List<Point>();
            }
            List<Point> locations = new List<Point>();
            while (maxSameRepeatCount-- > 0)
            {
                sourceMat = BitmapConverter.ToMat(searchAndCopyImage);
                var match = sourceMat.MatchTemplate(targetMat, TemplateMatchModes.CCoeffNormed);
                Cv2.MinMaxLoc(match, out _, out double max, out _, out Point maxLoc);
                max *= 100;
                if (similarity < max)
                {
                    locations.Add(new Point()
                    {
                        X = maxLoc.X,
                        Y = maxLoc.Y
                    });
                    using (var g = Graphics.FromImage(searchAndCopyImage))
                    {
                        using (var brush = new SolidBrush(Color.Black))
                        {
                            var rect = new Rectangle() { X = (int)maxLoc.X, Y = (int)maxLoc.Y, Width = target.Width, Height = target.Height };
                            g.FillRectangle(brush, rect);
                        }
                    }
                    using (var g = Graphics.FromImage(source))
                    {
                        if (isResultDisplay)
                        {
                            using (var pen = new Pen(Color.Red, 2))
                            {
                                g.DrawRectangle(pen, new Rectangle() { X = (int)maxLoc.X, Y = (int)maxLoc.Y, Width = target.Width, Height = target.Height });
                            }
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            return locations;
        }
        public static Bitmap CropImage(Bitmap source, Rect roiRect)
        {
            var sourceMat = BitmapConverter.ToMat(source);

            if (source.Width <= roiRect.Width || source.Height <= roiRect.Height)
            {
                return source;
            }

            var newRect = new OpenCvSharp.Rect()
            {
                Left = roiRect.Left,
                Top = roiRect.Top,
                Height = roiRect.Height,
                Width = roiRect.Width
            };
            using (var g = Graphics.FromImage(source))
            {
                using (var pen = new Pen(Color.LightPink))
                {
                    g.DrawRectangle(pen, new Rectangle() { X = newRect.Left, Y = newRect.Top, Width = newRect.Width, Height = newRect.Height });
                }
            }

            var roiMat = sourceMat.SubMat(newRect);
            var destBitmap = BitmapConverter.ToBitmap(roiMat);
            return destBitmap;
        }

        public static int SearchImagePercentage(Bitmap source, Tuple<double, double, double> lower, Tuple<double, double, double> upper)
        {
            var sourceMat = BitmapConverter.ToMat(source);
            var colorMat = sourceMat.CvtColor(ColorConversionCodes.RGB2HSV);
            var thresholded = new Mat();

            Cv2.InRange(colorMat,
                        new Scalar(lower.Item3, lower.Item1, lower.Item2),
                        new Scalar(upper.Item3, upper.Item1, upper.Item2),
                        thresholded);

            return Cv2.CountNonZero(thresholded) / (source.Width * source.Height);
        }
    }
}
