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
using Utils;
using Utils.Extensions;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Manager
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Singleton)]
    public class ScreenCaptureManager
    {
        public bool CaptureProcessWindow(Process process, out Bitmap bitmap)
        {
            bitmap = null;
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
            var isAnyMonitorOn = GetMonitorInfo().Any(r => r.IsOn == true);
            if (isAnyMonitorOn == false)
            {
                return CaptureWindowWithDPI(targetHandle, useMonitorDPI, out bitmap);
            }
            if (NativeHelper.IsIconic(targetHandle) == true)
            {
                CaptureThumbnailForMinimizedWindow(targetHandle, out bitmap);
            }
            else
            {
                CaptureWindowWithDPI(targetHandle, useMonitorDPI, out bitmap);
            }
            return bitmap != null;
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

                    var monitorInfo = new MonitorInfo()
                    {
                        Rect = rect,
                        Index = index++,
                        DeviceName = displayDevice.DeviceName,
                        FriendlyName = displayDevice.DeviceString,
                        Dpi = NativeHelper.GetMonitorDPI(hMonitor),
                        IsOn = false,
                    };

                    if (NativeHelper.EnumDisplayDevices(null, (uint)index, ref displayDevice, 0))
                    {
                        monitorInfo.IsOn = (displayDevice.StateFlags & 0x00000001) != 0;
                    }

                    monitors.Add(monitorInfo);

                    return true;
                }, 0);
            return monitors;
        }
        private bool CaptureThumbnailForMinimizedWindow(IntPtr targetHandle,
                                    out Bitmap bitmap)
        {
            if (targetHandle == IntPtr.Zero)
            {
                bitmap = null;
                return false;
            }

            IntPtr drawHandle = ApplicationManager.Instance.GetDrawWindowHandle();
            IntPtr thumbHandle = IntPtr.Zero;
            try
            {
                if (NativeHelper.DwmRegisterThumbnail(drawHandle, targetHandle, out thumbHandle) != 0)
                {
                    bitmap = null;
                    return false;
                }

                NativeHelper.DwmQueryThumbnailSourceSize(thumbHandle, out InterSize size);
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
                NativeHelper.DwmUpdateThumbnailProperties(thumbHandle, ref props);

                var currentRect = new IntRect();
                NativeHelper.GetWindowRect(drawHandle, ref currentRect);

                currentRect.Right = currentRect.Left + size.X;
                currentRect.Bottom = currentRect.Top + size.Y;
                NativeHelper.SetWindowPos(drawHandle, currentRect);

                bitmap = new Bitmap(destRect.Width, destRect.Height, PixelFormat.Format32bppArgb);

                using (var gfxBmp = Graphics.FromImage(bitmap))
                {
                    IntPtr hdcBitmap = gfxBmp.GetHdc();
                    NativeHelper.PrintWindow(drawHandle, hdcBitmap, 0x02 | 0x03);
                    gfxBmp.ReleaseHdc(hdcBitmap);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
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
            if (targetHandle == IntPtr.Zero)
            {
                bitmap = null;
                return false;
            }
            try
            {
                bitmap = ApplyDPI(targetHandle, useMonitorDPI);
                using (var gfxBmp = Graphics.FromImage(bitmap))
                {
                    IntPtr hdcBitmap = gfxBmp.GetHdc();
                    NativeHelper.PrintWindow(targetHandle, hdcBitmap, 0x02 | 0x03);
                    gfxBmp.ReleaseHdc(hdcBitmap);
                    gfxBmp.Dispose();
                }
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                bitmap = null;
                return false;
            }
        }
        private Bitmap ApplyDPI(IntPtr hWnd, bool useMonitorDPI)
        {
            var factor = NativeHelper.GetSystemDPI();
            IntRect rect = new IntRect();
            NativeHelper.GetWindowRect(hWnd, ref rect);
            if (rect.Width == 0 || rect.Height == 0)
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
