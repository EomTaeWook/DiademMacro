using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
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
            int sourceWidth = source.Width;
            int sourceHeight = source.Height;
            int targetWidth = target.Width;
            int targetHeight = target.Height;
            float[] sourcePixels = ReadPixels(source, cancellationToken);
            float[] targetPixels = ReadPixels(target, cancellationToken);
            int pixelCount = targetWidth * targetHeight;
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
            float[] centeredTargetPixels = new float[targetPixels.Length];
            for (int pixelIndex = 0; pixelIndex < targetPixels.Length; pixelIndex++)
            {
                if (pixelIndex % 4096 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                double centeredValue = targetPixels[pixelIndex] - channelMeans[pixelIndex % channelCount];
                centeredTargetPixels[pixelIndex] = (float)centeredValue;
                targetVariance += centeredValue * centeredValue;
            }
            location = new Point2D();
            if (targetVariance < double.Epsilon)
            {
                return 1;
            }
            if (TryFindExactMatch(sourcePixels, sourceWidth, sourceHeight, targetPixels, targetWidth, targetHeight, out location, cancellationToken) == true)
            {
                return 1;
            }
            int[] targetSamplePixelOffsets = CreateTargetSamplePixelOffsets(centeredTargetPixels, targetWidth, targetHeight, cancellationToken);
            Point2D sampledLocation = FindSampledLocation(sourcePixels, sourceWidth, sourceHeight, targetPixels, targetWidth, targetHeight, targetSamplePixelOffsets, cancellationToken);
            double sampledCorrelation = CalculateCorrelationAtPosition(sourcePixels, sourceWidth, centeredTargetPixels, targetWidth, targetHeight, sampledLocation.X, sampledLocation.Y, targetVariance, cancellationToken);
            if (Convert.ToInt32(sampledCorrelation * 100) == 100)
            {
                location = sampledLocation;
                return sampledCorrelation;
            }

            int integralWidth = sourceWidth + 1;
            int integralLength = integralWidth * (sourceHeight + 1);
            double[][] channelSums = new double[channelCount][];
            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                channelSums[channelIndex] = new double[integralLength];
            }
            double[] squaredSums = new double[integralLength];
            for (int row = 0; row < sourceHeight; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                double[] rowSums = new double[channelCount];
                double rowSquaredSum = 0;
                for (int column = 0; column < sourceWidth; column++)
                {
                    int pixelOffset = (row * sourceWidth + column) * channelCount;
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

            int targetRowLength = targetWidth * channelCount;
            int maximumTopPosition = sourceHeight - targetHeight;
            int maximumLeftPosition = sourceWidth - targetWidth;
            double[] maximumCorrelationsByTopPosition = new double[maximumTopPosition + 1];
            int[] maximumLeftPositionsByTopPosition = new int[maximumTopPosition + 1];
            Array.Fill(maximumCorrelationsByTopPosition, double.NegativeInfinity);
            int maximumWorkerCount = Math.Max(1, Environment.ProcessorCount - 1);
            ParallelOptions parallelOptions = new()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maximumWorkerCount
            };
            Parallel.For(0, maximumTopPosition + 1, parallelOptions, topPosition =>
            {
                double maximumCorrelationForTopPosition = double.NegativeInfinity;
                int maximumLeftPositionForTopPosition = 0;
                for (int leftPosition = 0; leftPosition <= maximumLeftPosition; leftPosition++)
                {
                    if (leftPosition % 32 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    double sourceVariance = SumRegion(squaredSums, integralWidth, leftPosition, topPosition, targetWidth, targetHeight);
                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        double channelSum = SumRegion(channelSums[channelIndex], integralWidth, leftPosition, topPosition, targetWidth, targetHeight);
                        sourceVariance -= channelSum * channelSum / pixelCount;
                    }
                    double correlation = 0;
                    if (sourceVariance > 0)
                    {
                        double numerator = 0;
                        for (int rowIndex = 0; rowIndex < targetHeight; rowIndex++)
                        {
                            int sourceOffset = ((topPosition + rowIndex) * sourceWidth + leftPosition) * channelCount;
                            int targetOffset = rowIndex * targetRowLength;
                            int columnIndex = 0;
                            for (; columnIndex <= targetRowLength - Vector<float>.Count; columnIndex += Vector<float>.Count)
                            {
                                Vector<float> sourceValues = new(sourcePixels, sourceOffset + columnIndex);
                                Vector<float> targetValues = new(centeredTargetPixels, targetOffset + columnIndex);
                                numerator += Vector.Dot(sourceValues, targetValues);
                            }
                            for (; columnIndex < targetRowLength; columnIndex++)
                            {
                                numerator += sourcePixels[sourceOffset + columnIndex] * centeredTargetPixels[targetOffset + columnIndex];
                            }
                        }
                        correlation = Math.Clamp(numerator / Math.Sqrt(sourceVariance * targetVariance), -1, 1);
                    }
                    if (correlation > maximumCorrelationForTopPosition)
                    {
                        maximumCorrelationForTopPosition = correlation;
                        maximumLeftPositionForTopPosition = leftPosition;
                    }
                }
                maximumCorrelationsByTopPosition[topPosition] = maximumCorrelationForTopPosition;
                maximumLeftPositionsByTopPosition[topPosition] = maximumLeftPositionForTopPosition;
            });

            double maximum = double.NegativeInfinity;
            for (int topPosition = 0; topPosition <= maximumTopPosition; topPosition++)
            {
                double correlation = maximumCorrelationsByTopPosition[topPosition];
                if (correlation > maximum)
                {
                    maximum = correlation;
                    location = new Point2D(maximumLeftPositionsByTopPosition[topPosition], topPosition);
                }
            }
            return maximum;
        }

        private static int[] CreateTargetSamplePixelOffsets(float[] centeredTargetPixels, int targetWidth, int targetHeight, CancellationToken cancellationToken)
        {
            const int channelCount = 4;
            int sampleRowCount = Math.Min(4, targetHeight);
            int sampleColumnCount = Math.Min(8, targetWidth);
            int[] targetSamplePixelOffsets = new int[sampleRowCount * sampleColumnCount];
            int sampleIndex = 0;
            for (int sampleRowIndex = 0; sampleRowIndex < sampleRowCount; sampleRowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int startRow = sampleRowIndex * targetHeight / sampleRowCount;
                int endRow = (sampleRowIndex + 1) * targetHeight / sampleRowCount;
                for (int sampleColumnIndex = 0; sampleColumnIndex < sampleColumnCount; sampleColumnIndex++)
                {
                    int startColumn = sampleColumnIndex * targetWidth / sampleColumnCount;
                    int endColumn = (sampleColumnIndex + 1) * targetWidth / sampleColumnCount;
                    double maximumPixelVariance = double.NegativeInfinity;
                    int selectedPixelOffset = startRow * targetWidth + startColumn;
                    for (int rowIndex = startRow; rowIndex < endRow; rowIndex++)
                    {
                        for (int columnIndex = startColumn; columnIndex < endColumn; columnIndex++)
                        {
                            int pixelOffset = (rowIndex * targetWidth + columnIndex) * channelCount;
                            double pixelVariance = 0;
                            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                            {
                                double centeredValue = centeredTargetPixels[pixelOffset + channelIndex];
                                pixelVariance += centeredValue * centeredValue;
                            }
                            if (pixelVariance > maximumPixelVariance)
                            {
                                maximumPixelVariance = pixelVariance;
                                selectedPixelOffset = rowIndex * targetWidth + columnIndex;
                            }
                        }
                    }
                    targetSamplePixelOffsets[sampleIndex] = selectedPixelOffset;
                    sampleIndex++;
                }
            }
            return targetSamplePixelOffsets;
        }

        private static Point2D FindSampledLocation(float[] sourcePixels, int sourceWidth, int sourceHeight, float[] targetPixels, int targetWidth, int targetHeight, int[] targetSamplePixelOffsets, CancellationToken cancellationToken)
        {
            const int channelCount = 4;
            int maximumTopPosition = sourceHeight - targetHeight;
            int maximumLeftPosition = sourceWidth - targetWidth;
            double[] minimumDifferencesByTopPosition = new double[maximumTopPosition + 1];
            int[] minimumLeftPositionsByTopPosition = new int[maximumTopPosition + 1];
            Array.Fill(minimumDifferencesByTopPosition, double.PositiveInfinity);
            int maximumWorkerCount = Math.Max(1, Environment.ProcessorCount - 1);
            ParallelOptions parallelOptions = new()
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = maximumWorkerCount
            };
            Parallel.For(0, maximumTopPosition + 1, parallelOptions, topPosition =>
            {
                double minimumDifferenceForTopPosition = double.PositiveInfinity;
                int minimumLeftPositionForTopPosition = 0;
                for (int leftPosition = 0; leftPosition <= maximumLeftPosition; leftPosition++)
                {
                    if (leftPosition % 64 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    double totalDifference = 0;
                    for (int sampleIndex = 0; sampleIndex < targetSamplePixelOffsets.Length; sampleIndex++)
                    {
                        int targetPixelOffset = targetSamplePixelOffsets[sampleIndex];
                        int targetRow = targetPixelOffset / targetWidth;
                        int targetColumn = targetPixelOffset - targetRow * targetWidth;
                        int sourceValueOffset = ((topPosition + targetRow) * sourceWidth + leftPosition + targetColumn) * channelCount;
                        int targetValueOffset = targetPixelOffset * channelCount;
                        for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                        {
                            double difference = sourcePixels[sourceValueOffset + channelIndex] - targetPixels[targetValueOffset + channelIndex];
                            totalDifference += difference * difference;
                        }
                        if (totalDifference >= minimumDifferenceForTopPosition)
                        {
                            break;
                        }
                    }
                    if (totalDifference < minimumDifferenceForTopPosition)
                    {
                        minimumDifferenceForTopPosition = totalDifference;
                        minimumLeftPositionForTopPosition = leftPosition;
                    }
                }
                minimumDifferencesByTopPosition[topPosition] = minimumDifferenceForTopPosition;
                minimumLeftPositionsByTopPosition[topPosition] = minimumLeftPositionForTopPosition;
            });

            double minimumDifference = double.PositiveInfinity;
            Point2D sampledLocation = new();
            for (int topPosition = 0; topPosition <= maximumTopPosition; topPosition++)
            {
                double difference = minimumDifferencesByTopPosition[topPosition];
                if (difference < minimumDifference)
                {
                    minimumDifference = difference;
                    sampledLocation = new Point2D(minimumLeftPositionsByTopPosition[topPosition], topPosition);
                }
            }
            return sampledLocation;
        }

        private static double CalculateCorrelationAtPosition(float[] sourcePixels, int sourceWidth, float[] centeredTargetPixels, int targetWidth, int targetHeight, int leftPosition, int topPosition, double targetVariance, CancellationToken cancellationToken)
        {
            const int channelCount = 4;
            int pixelCount = targetWidth * targetHeight;
            double[] sourceChannelSums = new double[channelCount];
            double sourceSquaredSum = 0;
            double numerator = 0;
            for (int rowIndex = 0; rowIndex < targetHeight; rowIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int columnIndex = 0; columnIndex < targetWidth; columnIndex++)
                {
                    int sourcePixelOffset = ((topPosition + rowIndex) * sourceWidth + leftPosition + columnIndex) * channelCount;
                    int targetPixelOffset = (rowIndex * targetWidth + columnIndex) * channelCount;
                    for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                    {
                        double sourceValue = sourcePixels[sourcePixelOffset + channelIndex];
                        sourceChannelSums[channelIndex] += sourceValue;
                        sourceSquaredSum += sourceValue * sourceValue;
                        numerator += sourceValue * centeredTargetPixels[targetPixelOffset + channelIndex];
                    }
                }
            }
            double sourceVariance = sourceSquaredSum;
            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                sourceVariance -= sourceChannelSums[channelIndex] * sourceChannelSums[channelIndex] / pixelCount;
            }
            if (sourceVariance <= 0)
            {
                return 0;
            }
            return Math.Clamp(numerator / Math.Sqrt(sourceVariance * targetVariance), -1, 1);
        }

        private static bool TryFindExactMatch(float[] sourcePixels, int sourceWidth, int sourceHeight, float[] targetPixels, int targetWidth, int targetHeight, out Point2D location, CancellationToken cancellationToken)
        {
            const int channelCount = 4;
            location = new Point2D();
            int maximumTopPosition = sourceHeight - targetHeight;
            int maximumLeftPosition = sourceWidth - targetWidth;
            int targetRowLength = targetWidth * channelCount;
            int targetCenterRow = targetHeight / 2;
            int targetCenterColumn = targetWidth / 2;
            int targetCenterPixelOffset = (targetCenterRow * targetWidth + targetCenterColumn) * channelCount;
            int targetLastPixelOffset = (targetWidth * targetHeight - 1) * channelCount;
            for (int topPosition = 0; topPosition <= maximumTopPosition; topPosition++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int leftPosition = 0; leftPosition <= maximumLeftPosition; leftPosition++)
                {
                    if (leftPosition % 64 == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    int sourceFirstPixelOffset = (topPosition * sourceWidth + leftPosition) * channelCount;
                    if (ArePixelsEqual(sourcePixels, sourceFirstPixelOffset, targetPixels, 0) == false)
                    {
                        continue;
                    }
                    int sourceCenterPixelOffset = ((topPosition + targetCenterRow) * sourceWidth + leftPosition + targetCenterColumn) * channelCount;
                    if (ArePixelsEqual(sourcePixels, sourceCenterPixelOffset, targetPixels, targetCenterPixelOffset) == false)
                    {
                        continue;
                    }
                    int sourceLastPixelOffset = ((topPosition + targetHeight - 1) * sourceWidth + leftPosition + targetWidth - 1) * channelCount;
                    if (ArePixelsEqual(sourcePixels, sourceLastPixelOffset, targetPixels, targetLastPixelOffset) == false)
                    {
                        continue;
                    }

                    bool isExactMatch = true;
                    for (int rowIndex = 0; rowIndex < targetHeight; rowIndex++)
                    {
                        int sourceOffset = ((topPosition + rowIndex) * sourceWidth + leftPosition) * channelCount;
                        int targetOffset = rowIndex * targetRowLength;
                        if (sourcePixels.AsSpan(sourceOffset, targetRowLength).SequenceEqual(targetPixels.AsSpan(targetOffset, targetRowLength)) == true)
                        {
                            continue;
                        }
                        isExactMatch = false;
                        break;
                    }
                    if (isExactMatch == false)
                    {
                        continue;
                    }
                    location = new Point2D(leftPosition, topPosition);
                    return true;
                }
            }
            return false;
        }

        private static bool ArePixelsEqual(float[] sourcePixels, int sourceOffset, float[] targetPixels, int targetOffset)
        {
            const int channelCount = 4;
            for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
            {
                if (sourcePixels[sourceOffset + channelIndex] != targetPixels[targetOffset + channelIndex])
                {
                    return false;
                }
            }
            return true;
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
