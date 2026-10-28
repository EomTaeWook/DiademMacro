using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using Utils.Infrastructure;
using IntRect = Utils.Infrastructure.IntRect;
using Rectangle = System.Drawing.Rectangle;

namespace Macro.Infrastructure
{
    public class OpenCVHelper
    {
        public static int Search(Bitmap source, Bitmap target, out Point2D location, bool isResultDisplay = false)
        {
            return Search(source, target, out location, CancellationToken.None, isResultDisplay);
        }

        public static int Search(Bitmap source, Bitmap target, out Point2D location, CancellationToken cancellationToken, bool isResultDisplay = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            location = new Point2D();
            if (source.Width < target.Width)
            {
                return 0;
            }
            if (source.Height < target.Height)
            {
                return 0;
            }
            double maximum = FindBestMatch(source, target, out location, cancellationToken);
            if (isResultDisplay == true)
            {
                DrawMatch(source, target, location);
            }
            return Convert.ToInt32(maximum * 100);
        }

        public static List<Point2D> MultipleSearch(Bitmap source, Bitmap target, int similarity, int maxSameRepeatCount, bool isResultDisplay = false)
        {
            return MultipleSearch(source, target, similarity, maxSameRepeatCount, CancellationToken.None, isResultDisplay);
        }

        public static List<Point2D> MultipleSearch(Bitmap source, Bitmap target, int similarity, int maxSameRepeatCount, CancellationToken cancellationToken, bool isResultDisplay = false)
        {
            cancellationToken.ThrowIfCancellationRequested();
            List<Point2D> locations = new();
            if (source.Width < target.Width)
            {
                return locations;
            }
            if (source.Height < target.Height)
            {
                return locations;
            }
            using Bitmap searchImage = new(source);
            while (maxSameRepeatCount-- > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double maximum = FindBestMatch(searchImage, target, out Point2D location, cancellationToken);
                if (similarity >= maximum * 100)
                {
                    break;
                }
                locations.Add(location);
                using (Graphics graphics = Graphics.FromImage(searchImage))
                {
                    graphics.FillRectangle(Brushes.Black, location.X, location.Y, target.Width, target.Height);
                }
                if (isResultDisplay == true)
                {
                    DrawMatch(source, target, location);
                }
            }
            return locations;
        }

        private static double FindBestMatch(Bitmap source, Bitmap target, out Point2D location, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            const int channelCount = 4;
            float[] sourcePixels = ReadPixels(source, cancellationToken);
            float[] targetPixels = ReadPixels(target, cancellationToken);
            int pixelCount = target.Width * target.Height;
            double[] channelMeans = new double[channelCount];
            for (int pixelIndex = 0; pixelIndex < targetPixels.Length; pixelIndex++)
            {
                if (pixelIndex % 4096 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                channelMeans[pixelIndex % channelCount] += targetPixels[pixelIndex];
            }
            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                channelMeans[channelIndex] /= pixelCount;
            }
            double targetVariance = 0;
            for (int pixelIndex = 0; pixelIndex < targetPixels.Length; pixelIndex++)
            {
                if (pixelIndex % 4096 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                double centeredValue = targetPixels[pixelIndex] - channelMeans[pixelIndex % channelCount];
                targetPixels[pixelIndex] = (float)centeredValue;
                targetVariance += centeredValue * centeredValue;
            }
            location = new Point2D();
            if (targetVariance < double.Epsilon)
            {
                return 1;
            }

            int integralWidth = source.Width + 1;
            int integralLength = integralWidth * (source.Height + 1);
            double[][] channelSums = new double[channelCount][];
            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                channelSums[channelIndex] = new double[integralLength];
            }
            double[] squaredSums = new double[integralLength];
            for (int row = 0; row < source.Height; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double[] rowSums = new double[channelCount];
                double rowSquaredSum = 0;
                for (int column = 0; column < source.Width; column++)
                {
                    int pixelOffset = (row * source.Width + column) * channelCount;
                    int integralOffset = (row + 1) * integralWidth + column + 1;
                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        double value = sourcePixels[pixelOffset + channelIndex];
                        rowSums[channelIndex] += value;
                        rowSquaredSum += value * value;
                        channelSums[channelIndex][integralOffset] = channelSums[channelIndex][integralOffset - integralWidth] + rowSums[channelIndex];
                    }
                    squaredSums[integralOffset] = squaredSums[integralOffset - integralWidth] + rowSquaredSum;
                }
            }

            double maximum = double.NegativeInfinity;
            int targetRowLength = target.Width * channelCount;
            for (int top = 0; top <= source.Height - target.Height; top++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int left = 0; left <= source.Width - target.Width; left++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    double sourceVariance = SumRegion(squaredSums, integralWidth, left, top, target.Width, target.Height);
                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        double channelSum = SumRegion(channelSums[channelIndex], integralWidth, left, top, target.Width, target.Height);
                        sourceVariance -= channelSum * channelSum / pixelCount;
                    }
                    double correlation = 0;
                    if (sourceVariance > 0)
                    {
                        double numerator = 0;
                        for (int row = 0; row < target.Height; row++)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            int sourceOffset = ((top + row) * source.Width + left) * channelCount;
                            int targetOffset = row * targetRowLength;
                            int column = 0;
                            for (; column <= targetRowLength - Vector<float>.Count; column += Vector<float>.Count)
                            {
                                Vector<float> sourceValues = new(sourcePixels, sourceOffset + column);
                                Vector<float> targetValues = new(targetPixels, targetOffset + column);
                                numerator += Vector.Dot(sourceValues, targetValues);
                            }
                            for (; column < targetRowLength; column++)
                            {
                                numerator += sourcePixels[sourceOffset + column] * targetPixels[targetOffset + column];
                            }
                        }
                        correlation = Math.Clamp(numerator / Math.Sqrt(sourceVariance * targetVariance), -1, 1);
                    }
                    if (correlation > maximum)
                    {
                        maximum = correlation;
                        location = new Point2D(left, top);
                    }
                }
            }
            return maximum;
        }

        private static double SumRegion(double[] sums, int stride, int left, int top, int width, int height)
        {
            int topLeft = top * stride + left;
            int bottomLeft = (top + height) * stride + left;
            return sums[bottomLeft + width] - sums[bottomLeft] - sums[topLeft + width] + sums[topLeft];
        }

        private static float[] ReadPixels(Bitmap bitmap, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using Bitmap normalizedBitmap = new(bitmap.Width, bitmap.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(normalizedBitmap))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.DrawImageUnscaled(bitmap, 0, 0);
            }
            Rectangle rectangle = new(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapData = normalizedBitmap.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            try
            {
                int rowLength = bitmap.Width * 4;
                byte[] rowBytes = new byte[rowLength];
                float[] pixels = new float[rowLength * bitmap.Height];
                for (int row = 0; row < bitmap.Height; row++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Marshal.Copy(IntPtr.Add(bitmapData.Scan0, row * bitmapData.Stride), rowBytes, 0, rowLength);
                    for (int column = 0; column < rowLength; column++)
                    {
                        pixels[row * rowLength + column] = rowBytes[column];
                    }
                }
                return pixels;
            }
            finally
            {
                normalizedBitmap.UnlockBits(bitmapData);
            }
        }

        private static void DrawMatch(Bitmap source, Bitmap target, Point2D location)
        {
            using Graphics graphics = Graphics.FromImage(source);
            using Pen pen = new(Color.Red, 2);
            graphics.DrawRectangle(pen, location.X, location.Y, target.Width, target.Height);
        }

        public static Bitmap CropImage(Bitmap source, IntRect roiRect)
        {
            if (source.Width <= roiRect.Width)
            {
                return source;
            }
            if (source.Height <= roiRect.Height)
            {
                return source;
            }
            Rectangle rectangle = new(roiRect.Left, roiRect.Top, roiRect.Width, roiRect.Height);
            Bitmap croppedImage = source.Clone(rectangle, source.PixelFormat);
            using Graphics graphics = Graphics.FromImage(source);
            using Pen pen = new(Color.LightPink);
            graphics.DrawRectangle(pen, rectangle);
            return croppedImage;
        }

        public static int SearchImagePercentage(Bitmap source, Tuple<double, double, double> lower, Tuple<double, double, double> upper)
        {
            float[] pixels = ReadPixels(source, CancellationToken.None);
            int matchedPixels = 0;
            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex += 4)
            {
                // Preserve the original RGB2HSV interpretation of the bitmap's BGR channels.
                Color color = Color.FromArgb((int)pixels[pixelIndex], (int)pixels[pixelIndex + 1], (int)pixels[pixelIndex + 2]);
                double maximum = Math.Max(color.R, Math.Max(color.G, color.B));
                double minimum = Math.Min(color.R, Math.Min(color.G, color.B));
                double saturation = 0;
                if (maximum > 0)
                {
                    saturation = Math.Round((maximum - minimum) * 255 / maximum);
                }
                double hue = Math.Round(color.GetHue() / 2);
                if (hue >= 180)
                {
                    hue = 0;
                }
                if (hue < lower.Item3)
                {
                    continue;
                }
                if (hue > upper.Item3)
                {
                    continue;
                }
                if (saturation < lower.Item1)
                {
                    continue;
                }
                if (saturation > upper.Item1)
                {
                    continue;
                }
                if (maximum < lower.Item2)
                {
                    continue;
                }
                if (maximum > upper.Item2)
                {
                    continue;
                }
                matchedPixels++;
            }
            return matchedPixels / (source.Width * source.Height);
        }
    }
}
