using DataContainer.Generated;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Utils;
using Utils.Extensions;
using Utils.Infrastructure;
using WinRT;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class ScreenCaptureManager
    {
        private const int CaptureFrameTimeoutMilliseconds = 2000;
        private const uint Direct3D11SdkVersion = 7;
        private const uint Direct3D11CreateDeviceBgraSupport = 0x20;
        private static readonly Guid GraphicsCaptureItemInterfaceId = new Guid("79C3F95B-31F7-4EC2-A464-632EF5D30760");
        private static readonly Guid DxgiDeviceInterfaceId = new Guid("54EC77FA-1377-44E6-8C32-88FD5F44C84C");
        private readonly object _graphicsCaptureDeviceLock = new object();
        private IntPtr _drawWindowHandle;
        private IDirect3DDevice _graphicsCaptureDevice;
        private bool _graphicsCaptureFailureLogged;

        private enum Direct3DDriverType : uint
        {
            Unknown = 0,
            Hardware = 1
        }

        [ComImport]
        [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [ComVisible(true)]
        private interface IGraphicsCaptureItemInterop
        {
            IntPtr CreateForWindow([In] IntPtr windowHandle, [In] ref Guid interfaceId);
            IntPtr CreateForMonitor([In] IntPtr monitorHandle, [In] ref Guid interfaceId);
        }

        [DllImport("d3d11.dll", ExactSpelling = true)]
        private static extern int D3D11CreateDevice(IntPtr adapter, Direct3DDriverType driverType, IntPtr softwareRasterizer, uint flags, IntPtr featureLevels, uint featureLevelCount, uint sdkVersion, out IntPtr device, out int featureLevel, out IntPtr immediateContext);

        [DllImport("d3d11.dll", ExactSpelling = true)]
        private static extern int CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice, out IntPtr graphicsDevice);

        public void SetDrawWindowHandle(IntPtr drawWindowHandle)
        {
            _drawWindowHandle = drawWindowHandle;
        }

        public bool CaptureProcessWindow(Process process, CancellationToken cancellationToken, out Bitmap bitmap)
        {
            bitmap = null;
            if (cancellationToken.IsCancellationRequested == true)
            {
                return false;
            }
            IntPtr targetHandle = process.MainWindowHandle;
            if (targetHandle == IntPtr.Zero)
            {
                return false;
            }
            var template = TemplateContainer<ApplicationTemplate>.Find(process.ProcessName);
            var useMonitorDPI = false;
            if (template.Invalid() == false)
            {
                useMonitorDPI = template.UseMonitorDPI;
            }
            if (NativeHelper.IsIconic(targetHandle) == true)
            {
                bool isAnyMonitorOn = GetMonitorInfo().Any(monitorInfo => monitorInfo.IsOn == true);
                if (isAnyMonitorOn == false)
                {
                    return CaptureWindowWithDPI(targetHandle, useMonitorDPI, out bitmap);
                }
                return CaptureThumbnailForMinimizedWindow(targetHandle, out bitmap);
            }
            if (CaptureWindowWithWindowsGraphicsCapture(targetHandle, cancellationToken, out bitmap) == true)
            {
                _graphicsCaptureFailureLogged = false;
                return true;
            }
            if (cancellationToken.IsCancellationRequested == true)
            {
                return false;
            }
            return CaptureWindowWithDPI(targetHandle, useMonitorDPI, out bitmap);
        }

        public IntRect AdjustRectForDPI(Utils.Infrastructure.Rectangle rect, MonitorInfo monitorInfo)
        {
            var factor = NativeHelper.GetSystemDPI();
            var factorX = 1.0F * factor.X / monitorInfo.Dpi.X;
            var factorY = 1.0F * factor.Y / monitorInfo.Dpi.Y;

            var newRect = new IntRect()
            {
                Left = Convert.ToInt32(rect.Left * factorX),
                Top = Convert.ToInt32(rect.Top * factorY),
                Bottom = Convert.ToInt32(rect.Bottom * factorY),
                Right = Convert.ToInt32(rect.Right * factorX)
            };
            return newRect;
        }

        public List<MonitorInfo> GetMonitorInfo()
        {
            var monitors = new List<MonitorInfo>();
            int index = 0;

            NativeHelper.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref IntRect rect, int data) =>
                {
                    DisplayDevice displayDevice = new DisplayDevice();
                    displayDevice.cb = Marshal.SizeOf(displayDevice);
                    int monitorIndex = index;
                    index++;
                    bool displayDeviceFound = NativeHelper.EnumDisplayDevices(null, (uint)monitorIndex, ref displayDevice, 0);
                    bool isOn = false;
                    if (displayDeviceFound == true)
                    {
                        isOn = (displayDevice.StateFlags & 0x00000001) != 0;
                    }

                    var monitorInfo = new MonitorInfo()
                    {
                        Rect = rect,
                        Index = monitorIndex,
                        DeviceName = displayDeviceFound ? displayDevice.DeviceName : string.Empty,
                        FriendlyName = displayDeviceFound ? displayDevice.DeviceString : string.Empty,
                        Dpi = NativeHelper.GetMonitorDPI(hMonitor),
                        IsOn = isOn,
                    };

                    monitors.Add(monitorInfo);

                    return true;
                }, 0);
            return monitors;
        }

        private bool CaptureWindowWithWindowsGraphicsCapture(IntPtr targetHandle, CancellationToken cancellationToken, out Bitmap bitmap)
        {
            bitmap = null;
            try
            {
                if (GraphicsCaptureSession.IsSupported() == false)
                {
                    return false;
                }
                GraphicsCaptureItem captureItem = CreateGraphicsCaptureItem(targetHandle);
                if (captureItem == null)
                {
                    return false;
                }
                SizeInt32 captureSize = captureItem.Size;
                if (captureSize.Width <= 0)
                {
                    return false;
                }
                if (captureSize.Height <= 0)
                {
                    return false;
                }
                IDirect3DDevice graphicsCaptureDevice = GetGraphicsCaptureDevice();
                using Direct3D11CaptureFramePool framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(graphicsCaptureDevice, DirectXPixelFormat.B8G8R8A8UIntNormalized, 1, captureSize);
                using GraphicsCaptureSession captureSession = framePool.CreateCaptureSession(captureItem);
                captureSession.IsCursorCaptureEnabled = false;
                TaskCompletionSource<bool> frameArrivedCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                TypedEventHandler<Direct3D11CaptureFramePool, object> frameArrivedHandler = (sender, eventArgs) => frameArrivedCompletionSource.TrySetResult(true);
                framePool.FrameArrived += frameArrivedHandler;
                try
                {
                    captureSession.StartCapture();
                    frameArrivedCompletionSource.Task.WaitAsync(TimeSpan.FromMilliseconds(CaptureFrameTimeoutMilliseconds), cancellationToken).GetAwaiter().GetResult();
                    using Direct3D11CaptureFrame captureFrame = framePool.TryGetNextFrame();
                    if (captureFrame == null)
                    {
                        return false;
                    }
                    Windows.Foundation.IAsyncOperation<SoftwareBitmap> copyOperation = SoftwareBitmap.CreateCopyFromSurfaceAsync(captureFrame.Surface, BitmapAlphaMode.Ignore);
                    using SoftwareBitmap softwareBitmap = System.WindowsRuntimeSystemExtensions.AsTask(copyOperation, cancellationToken).GetAwaiter().GetResult();
                    bitmap = ConvertToDrawingBitmap(softwareBitmap);
                    return bitmap != null;
                }
                finally
                {
                    framePool.FrameArrived -= frameArrivedHandler;
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception exception)
            {
                LogGraphicsCaptureFailure(exception);
                bitmap?.Dispose();
                bitmap = null;
                return false;
            }
        }

        private void LogGraphicsCaptureFailure(Exception exception)
        {
            if (_graphicsCaptureFailureLogged == true)
            {
                return;
            }
            _graphicsCaptureFailureLogged = true;
            LogHelper.Debug($"Windows Graphics Capture failed. {exception}");
        }

        private IDirect3DDevice GetGraphicsCaptureDevice()
        {
            lock (_graphicsCaptureDeviceLock)
            {
                if (_graphicsCaptureDevice == null)
                {
                    _graphicsCaptureDevice = CreateGraphicsCaptureDevice();
                }
                return _graphicsCaptureDevice;
            }
        }

        private static IDirect3DDevice CreateGraphicsCaptureDevice()
        {
            IntPtr nativeDevice = IntPtr.Zero;
            IntPtr immediateContext = IntPtr.Zero;
            IntPtr dxgiDevice = IntPtr.Zero;
            IntPtr graphicsDevice = IntPtr.Zero;
            try
            {
                int createDeviceResult = D3D11CreateDevice(IntPtr.Zero, Direct3DDriverType.Hardware, IntPtr.Zero, Direct3D11CreateDeviceBgraSupport, IntPtr.Zero, 0, Direct3D11SdkVersion, out nativeDevice, out _, out immediateContext);
                Marshal.ThrowExceptionForHR(createDeviceResult);
                Guid dxgiDeviceInterfaceId = DxgiDeviceInterfaceId;
                int queryInterfaceResult = Marshal.QueryInterface(nativeDevice, in dxgiDeviceInterfaceId, out dxgiDevice);
                Marshal.ThrowExceptionForHR(queryInterfaceResult);
                int createGraphicsDeviceResult = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice, out graphicsDevice);
                Marshal.ThrowExceptionForHR(createGraphicsDeviceResult);
                return MarshalInterface<IDirect3DDevice>.FromAbi(graphicsDevice);
            }
            finally
            {
                ReleaseComPointer(ref graphicsDevice);
                ReleaseComPointer(ref dxgiDevice);
                ReleaseComPointer(ref immediateContext);
                ReleaseComPointer(ref nativeDevice);
            }
        }

        private static GraphicsCaptureItem CreateGraphicsCaptureItem(IntPtr targetHandle)
        {
            IGraphicsCaptureItemInterop captureItemInterop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
            Guid graphicsCaptureItemInterfaceId = GraphicsCaptureItemInterfaceId;
            IntPtr captureItemPointer = captureItemInterop.CreateForWindow(targetHandle, ref graphicsCaptureItemInterfaceId);
            if (captureItemPointer == IntPtr.Zero)
            {
                return null;
            }
            try
            {
                return GraphicsCaptureItem.FromAbi(captureItemPointer);
            }
            finally
            {
                Marshal.Release(captureItemPointer);
            }
        }

        private static Bitmap ConvertToDrawingBitmap(SoftwareBitmap sourceSoftwareBitmap)
        {
            SoftwareBitmap convertedSoftwareBitmap = null;
            bool requiresConversion = false;
            if (sourceSoftwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
            {
                requiresConversion = true;
            }
            if (sourceSoftwareBitmap.BitmapAlphaMode != BitmapAlphaMode.Ignore)
            {
                requiresConversion = true;
            }
            if (requiresConversion == true)
            {
                convertedSoftwareBitmap = SoftwareBitmap.Convert(sourceSoftwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);
            }
            SoftwareBitmap softwareBitmap = convertedSoftwareBitmap ?? sourceSoftwareBitmap;
            int width = softwareBitmap.PixelWidth;
            int height = softwareBitmap.PixelHeight;
            if (width <= 0)
            {
                convertedSoftwareBitmap?.Dispose();
                return null;
            }
            if (height <= 0)
            {
                convertedSoftwareBitmap?.Dispose();
                return null;
            }
            int sourceStride = checked(width * 4);
            byte[] pixelBytes = new byte[checked(sourceStride * height)];
            IBuffer pixelBuffer = pixelBytes.AsBuffer();
            softwareBitmap.CopyToBuffer(pixelBuffer);
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bitmapData = null;
            bool copySucceeded = false;
            try
            {
                bitmapData = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                for (int row = 0; row < height; ++row)
                {
                    IntPtr destinationRow = IntPtr.Add(bitmapData.Scan0, row * bitmapData.Stride);
                    Marshal.Copy(pixelBytes, row * sourceStride, destinationRow, sourceStride);
                }
                copySucceeded = true;
                return bitmap;
            }
            finally
            {
                if (bitmapData != null)
                {
                    bitmap.UnlockBits(bitmapData);
                }
                convertedSoftwareBitmap?.Dispose();
                if (copySucceeded == false)
                {
                    bitmap.Dispose();
                }
            }
        }

        private static void ReleaseComPointer(ref IntPtr pointer)
        {
            if (pointer == IntPtr.Zero)
            {
                return;
            }
            Marshal.Release(pointer);
            pointer = IntPtr.Zero;
        }

        private bool CaptureThumbnailForMinimizedWindow(IntPtr targetHandle, out Bitmap bitmap)
        {
            bitmap = null;
            if (targetHandle == IntPtr.Zero)
            {
                return false;
            }
            if (_drawWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            IntPtr drawHandle = _drawWindowHandle;
            IntPtr thumbHandle = IntPtr.Zero;
            try
            {
                if (NativeHelper.DwmRegisterThumbnail(drawHandle, targetHandle, out thumbHandle) != 0)
                {
                    return false;
                }

                if (NativeHelper.DwmQueryThumbnailSourceSize(thumbHandle, out InterSize size) != 0)
                {
                    return false;
                }
                if (size.X <= 0)
                {
                    return false;
                }
                if (size.Y <= 0)
                {
                    return false;
                }
                var destRect = new IntRect
                {
                    Right = size.X,
                    Bottom = size.Y
                };
                var props = new DWMThumbnailProperties
                {
                    SourceClientAreaOnly = false,
                    Visible = true,
                    Opacity = 255,
                    Destination = destRect,
                    Flags = (uint)(DWMThumbnailPropertiesType.SourcecLientareaOnly |
                                DWMThumbnailPropertiesType.Visible |
                                DWMThumbnailPropertiesType.Opacity |
                                DWMThumbnailPropertiesType.Rectdestination
                                )
                };
                if (NativeHelper.DwmUpdateThumbnailProperties(thumbHandle, ref props) != 0)
                {
                    return false;
                }

                var currentRect = new IntRect();
                if (NativeHelper.GetWindowRect(drawHandle, ref currentRect) == IntPtr.Zero)
                {
                    return false;
                }

                currentRect.Right = currentRect.Left + size.X;
                currentRect.Bottom = currentRect.Top + size.Y;
                if (NativeHelper.SetWindowPos(drawHandle, currentRect) == false)
                {
                    return false;
                }

                bitmap = new Bitmap(destRect.Width, destRect.Height, PixelFormat.Format32bppArgb);

                bool printSucceeded;
                using (Graphics bitmapGraphics = Graphics.FromImage(bitmap))
                {
                    IntPtr hdcBitmap = IntPtr.Zero;
                    try
                    {
                        hdcBitmap = bitmapGraphics.GetHdc();
                        printSucceeded = NativeHelper.PrintWindow(drawHandle, hdcBitmap, 0x02 | 0x03);
                    }
                    finally
                    {
                        if (hdcBitmap != IntPtr.Zero)
                        {
                            bitmapGraphics.ReleaseHdc(hdcBitmap);
                        }
                    }
                }
                if (printSucceeded == false)
                {
                    bitmap.Dispose();
                    bitmap = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                bitmap?.Dispose();
                bitmap = null;
                return false;
            }
            finally
            {
                if (thumbHandle != IntPtr.Zero)
                {
                    NativeHelper.DwmUnregisterThumbnail(thumbHandle);
                }
            }
        }
        private bool CaptureWindowWithDPI(IntPtr targetHandle, bool useMonitorDPI, out Bitmap bitmap)
        {
            bitmap = null;
            if (targetHandle == IntPtr.Zero)
            {
                return false;
            }
            try
            {
                bitmap = ApplyDPI(targetHandle, useMonitorDPI);
                if (bitmap == null)
                {
                    return false;
                }
                bool printSucceeded;
                using (Graphics bitmapGraphics = Graphics.FromImage(bitmap))
                {
                    IntPtr hdcBitmap = IntPtr.Zero;
                    try
                    {
                        hdcBitmap = bitmapGraphics.GetHdc();
                        printSucceeded = NativeHelper.PrintWindow(targetHandle, hdcBitmap, 0x02 | 0x03);
                    }
                    finally
                    {
                        if (hdcBitmap != IntPtr.Zero)
                        {
                            bitmapGraphics.ReleaseHdc(hdcBitmap);
                        }
                    }
                }
                if (printSucceeded == false)
                {
                    bitmap.Dispose();
                    bitmap = null;
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                LogHelper.Error(exception);
                bitmap?.Dispose();
                bitmap = null;
                return false;
            }
        }
        private Bitmap ApplyDPI(IntPtr hWnd, bool useMonitorDPI)
        {
            var factor = NativeHelper.GetSystemDPI();
            IntRect rect = new IntRect();
            if (NativeHelper.GetWindowRect(hWnd, ref rect) == IntPtr.Zero)
            {
                return null;
            }
            if (rect.Width <= 0)
            {
                return null;
            }
            if (rect.Height <= 0)
            {
                return null;
            }
            var factorX = 1.0F;
            var factorY = 1.0F;

            if (useMonitorDPI)
            {
                foreach (var monitor in GetMonitorInfo())
                {
                    if (monitor.Rect.IsContain(rect))
                    {
                        factorX = factor.X / (monitor.Dpi.X * factorX);
                        factorY = factor.Y / (monitor.Dpi.Y * factorY);
                        break;
                    }
                }
            }
            int adjustedWidth = (int)Math.Truncate(rect.Width * factorX);
            int adjustedHeight = (int)Math.Truncate(rect.Height * factorY);

            return new Bitmap(adjustedWidth, adjustedHeight, PixelFormat.Format32bppArgb);
        }
    }
}
