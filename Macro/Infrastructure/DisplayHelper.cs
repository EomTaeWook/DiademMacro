using Dignus.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using Utils;
using Utils.Extensions;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Manager
{
    public class DisplayHelper
    {
        public static Rect ApplyMonitorDPI(InnerRectangle rect, MonitorInfo monitorInfo)
        {
            var factor = NativeHelper.GetSystemDPI();
            var factorX = 1.0F * factor.X / monitorInfo.Dpi.X;
            var factorY = 1.0F * factor.Y / monitorInfo.Dpi.Y;

            var newRect = new Rect()
            {
                Left = Convert.ToInt32(rect.Left * factorX),
                Top = Convert.ToInt32(rect.Top * factorY),
                Bottom = Convert.ToInt32(rect.Bottom * factorY),
                Right = Convert.ToInt32(rect.Right * factorX)
            };
            return newRect;
        }
        public static List<MonitorInfo> MonitorInfo()
        {
            var monitors = new List<MonitorInfo>();
            int index = 0;

            NativeHelper.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                new NativeHelper.MonitorEnumDelegate((IntPtr hMonitor, IntPtr hdcMonitor, ref Rect rect, int data) =>
                {
                    monitors.Add(new MonitorInfo()
                    {
                        Rect = rect,
                        Index = index++,
                        Dpi = NativeHelper.GetMonitorDPI(hMonitor)
                    });
                    return true;
                }
            ), 0);
            return monitors;
        }
        public static Bitmap Capture(MonitorInfo monitor, Rect rect)
        {
            try
            {
                var factor = NativeHelper.GetSystemDPI();
                var factorX = 1.0F * factor.X / ConstHelper.DefaultDPI;
                var factorY = 1.0F * factor.Y / ConstHelper.DefaultDPI;

                Bitmap bmp = new Bitmap((int)Math.Truncate(rect.Width * factorX), (int)Math.Truncate(rect.Height * factorY));
                using (var g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(monitor.Rect.Left, monitor.Rect.Top,
                       (int)Math.Truncate(rect.Left * -1.0F * factorX), (int)Math.Truncate(rect.Top * -1.0F * factorY),
                        new Size(monitor.Rect.Width, monitor.Rect.Height),
                        CopyPixelOperation.SourceCopy);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return null;
            }
        }


        public static bool ProcessCaptureV2(Process process,
                                            IntPtr destHandle,
                                            out Bitmap bmp)
        {
            try
            {
                IntPtr targetHandle = process.MainWindowHandle;
                if (targetHandle == IntPtr.Zero)
                {
                    bmp = null;
                    return false;
                }

                if (NativeHelper.DwmRegisterThumbnail(destHandle, targetHandle, out IntPtr thumbHandle) == 0)
                {
                    NativeHelper.DwmQueryThumbnailSourceSize(thumbHandle, out InterSize size);

                    var destRect = new Rect
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
                    NativeHelper.DwmUpdateThumbnailProperties(thumbHandle, ref props);

                    var currentRect = new Rect();

                    NativeHelper.GetWindowRect(destHandle, ref currentRect);

                    currentRect.Right = currentRect.Left + size.X;
                    currentRect.Bottom = currentRect.Top + size.Y;

                    NativeHelper.SetWindowPos(destHandle, currentRect);

                    bmp = new Bitmap(destRect.Width, destRect.Height, PixelFormat.Format32bppArgb);

                    using (var gfxBmp = Graphics.FromImage(bmp))
                    {
                        IntPtr hdcBitmap = gfxBmp.GetHdc();
                        NativeHelper.PrintWindow(destHandle, hdcBitmap, 0x02 | 0x03);
                        gfxBmp.ReleaseHdc(hdcBitmap);
                        gfxBmp.Dispose();
                    }
                    NativeHelper.DwmUnregisterThumbnail(thumbHandle);
                }
                else
                {
                    bmp = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                bmp = null;
                return false;
            }
        }
        public static bool ProcessCapture(Process process, out Bitmap bmp, bool isDynamic = false)
        {
            try
            {
                IntPtr hWnd = process.MainWindowHandle;
                if (hWnd == IntPtr.Zero)
                {
                    bmp = null;
                    return false;
                }
                var factor = NativeHelper.GetSystemDPI();
                Rect rect = new Rect();
                NativeHelper.GetWindowRect(hWnd, ref rect);
                if (rect.Width == 0 || rect.Height == 0)
                {
                    bmp = null;
                    return false;
                }
                var factorX = 1.0F;
                var factorY = 1.0F;
                if (isDynamic)
                {
                    foreach (var monitor in MonitorInfo())
                    {
                        if (monitor.Rect.IsContain(rect))
                        {
                            factorX = factor.X / (monitor.Dpi.X * factorX);
                            factorY = factor.Y / (monitor.Dpi.Y * factorY);
                            break;
                        }
                    }
                }

                bmp = new Bitmap((int)Math.Truncate(rect.Width * factorX), (int)Math.Truncate(rect.Height * factorY), PixelFormat.Format32bppArgb);
                using (var gfxBmp = Graphics.FromImage(bmp))
                {
                    IntPtr hdcBitmap = gfxBmp.GetHdc();
                    NativeHelper.PrintWindow(hWnd, hdcBitmap, 0x02 | 0x03);
                    gfxBmp.ReleaseHdc(hdcBitmap);
                    gfxBmp.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                bmp = null;
                return false;
            }
        }
    }
}
