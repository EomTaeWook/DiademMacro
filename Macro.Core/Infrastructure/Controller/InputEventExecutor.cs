using DataContainer.Generated;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Extensions;
using Macro.Infrastructure.Interface;
using Macro.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Utils;
using Utils.Extensions;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Controller
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class InputEventExecutor
    {
        private readonly Random _randomGenerator;
        public IKeyboardInput Keyboard { get; private set; }
        public IMouseInput Mouse { get; private set; }
        public InputEventExecutor(IKeyboardInput keyboardInput, IMouseInput mouse)
        {
            Keyboard = keyboardInput;
            Mouse = mouse;
            _randomGenerator = new Random();
        }
        public int GetRandomValue(int minValue, int maxValue)
        {
            var choice = _randomGenerator.Next(0, 2);
            if (choice == 0)
            {
                return -_randomGenerator.Next(minValue, maxValue);
            }
            else
            {
                return _randomGenerator.Next(minValue, maxValue);
            }
        }
        public void ProcessMouseEvent(IntPtr hWnd,
            EventInfoModel model,
            Point2D matchedLocation,
            ApplicationTemplate applicationTemplate,
            int dragDelay)
        {
            var processLocation = new IntRect();
            NativeHelper.GetWindowRect(hWnd, ref processLocation);

            var currentProcessLocation = model.ProcessInfo.Position - processLocation;

            if (model.HardClick == false)
            {
                matchedLocation.X = applicationTemplate.OffsetX;
                matchedLocation.Y = applicationTemplate.OffsetY;
                ProcessMouseEvent(hWnd, matchedLocation, model, dragDelay);
            }
            else
            {
                var clickPoint = new Point2D
                {
                    X = model.MouseEventInfo.MousePoint.X - currentProcessLocation.Left,
                    Y = model.MouseEventInfo.MousePoint.Y - currentProcessLocation.Top
                };
                ProcessHardClick(clickPoint, model.MouseEventInfo.MouseEventType);
            }
        }

        public void ProcessRelativeToImageEvent(IntPtr hWnd, EventInfoModel model, Point2D matchedLocation, ApplicationTemplate applicationTemplate, Size capturedImageSize)
        {
            if (model.HardClick == true)
            {
                Point2D clickPoint = new(matchedLocation.X + model.Image.Width / 2 + model.PositionRelativeToImage.X, matchedLocation.Y + model.Image.Height / 2 + model.PositionRelativeToImage.Y);
                ProcessPhysicalImageClick(hWnd, clickPoint, capturedImageSize);
                return;
            }
            matchedLocation.X = (matchedLocation.X + applicationTemplate.OffsetX) + (model.Image.Width / 2);
            matchedLocation.Y = (matchedLocation.Y + applicationTemplate.OffsetY) + (model.Image.Height / 2);

            matchedLocation.X += model.PositionRelativeToImage.X;
            matchedLocation.Y += model.PositionRelativeToImage.Y;

            ProcessImageEvent(hWnd,
                matchedLocation);
        }

        public void ProcessImageEvent(IntPtr hWnd, EventInfoModel model, Point2D matchedLocation, ApplicationTemplate applicationTemplate, Size capturedImageSize)
        {
            if (model.HardClick == true)
            {
                Point2D clickPoint = new(matchedLocation.X + model.Image.Width / 2, matchedLocation.Y + model.Image.Height / 2);
                ProcessPhysicalImageClick(hWnd, clickPoint, capturedImageSize);
                return;
            }
            var percentageX = (int)_randomGenerator.NextDouble();
            var percentageY = (int)_randomGenerator.NextDouble();

            matchedLocation.X = (matchedLocation.X + applicationTemplate.OffsetX) + (model.Image.Width * percentageX);
            matchedLocation.Y = (matchedLocation.Y + applicationTemplate.OffsetY) + (model.Image.Height * percentageY);

            ProcessImageEvent(hWnd, matchedLocation);
        }

        private void ProcessPhysicalImageClick(IntPtr windowHandle, Point2D imageLocation, Size capturedImageSize)
        {
            IntPtr rootWindowHandle = NativeHelper.GetAncestor(windowHandle, NativeHelper.RootAncestor);
            if (rootWindowHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The target window is unavailable.");
            }
            if (NativeHelper.IsIconic(rootWindowHandle) == true)
            {
                throw new InvalidOperationException("The target window is minimized.");
            }
            if (NativeHelper.GetForegroundWindow() != rootWindowHandle)
            {
                NativeHelper.SetForegroundWindow(rootWindowHandle);
                if (NativeHelper.GetForegroundWindow() != rootWindowHandle)
                {
                    throw new InvalidOperationException("The target window could not be activated.");
                }
            }
            IntRect windowRectangle = new();
            if (NativeHelper.GetWindowRect(rootWindowHandle, ref windowRectangle) == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The target window bounds are unavailable.");
            }
            Point clickPoint = new(windowRectangle.Left + (int)Math.Round((double)imageLocation.X * windowRectangle.Width / capturedImageSize.Width), windowRectangle.Top + (int)Math.Round((double)imageLocation.Y * windowRectangle.Height / capturedImageSize.Height));
            ProcessHardClick(clickPoint, MouseEventType.LeftClick);
        }

        private void ProcessImageEvent(IntPtr hWnd,
                                        Point2D location)
        {
            LogHelper.Debug($">>>>Image Location X : {location.X} Location Y : {location.Y}");

            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, location.ToLParam());
            Task.Delay(10).GetAwaiter().GetResult();
            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, location.ToLParam());
        }
        public void ProcessHardClick(Point clickPoint, MouseEventType mouseEventType)
        {
            if (mouseEventType != MouseEventType.LeftClick)
            {
                if (mouseEventType != MouseEventType.RightClick)
                {
                    LogHelper.Error($"unsupported MouseEventType: {mouseEventType}");
                    return;
                }
            }
            Point currentPosition = NativeHelper.GetCursorPosition();
            if (NativeHelper.SetCursorPos(clickPoint.X, clickPoint.Y) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The cursor could not be moved to the click position.");
            }
            try
            {
                if (mouseEventType == MouseEventType.LeftClick)
                {
                    Mouse.LeftButtonDown();
                    try
                    {
                        Task.Delay(10).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        Mouse.LeftButtonUp();
                    }
                }
                else
                {
                    Mouse.RightButtonDown();
                    try
                    {
                        Task.Delay(10).GetAwaiter().GetResult();
                    }
                    finally
                    {
                        Mouse.RightButtonUp();
                    }
                }
            }
            finally
            {
                if (NativeHelper.SetCursorPos(currentPosition.X, currentPosition.Y) == 0)
                {
                    LogHelper.Error(new Win32Exception(Marshal.GetLastWin32Error(), "The cursor position could not be restored."));
                }
            }
        }
        public void ProcessSameImageMouseDragEvent(IntPtr hWnd,
                                            Point2D start,
                                            Point2D arrive,
                                            EventInfoModel model,
                                            int dragDelay)
        {
            LogHelper.Debug($">>>>Same Drag Mouse Start Target X : {arrive.X} Target Y : {arrive.Y}");
            var interval = 3;
            var middlePoints = this.GetIntevalDragMiddlePoint(start, arrive, interval);

            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, start.ToLParam());
            Task.Delay(10).GetAwaiter().GetResult();

            Point2D mousePosition;
            for (int i = 0; i < middlePoints.Count; ++i)
            {
                mousePosition = new Point2D()
                {
                    X = Math.Abs(model.ProcessInfo.Position.Left + middlePoints[i].X * -1),
                    Y = Math.Abs(model.ProcessInfo.Position.Top + middlePoints[i].Y * -1)
                };
                LogHelper.Debug($">>>>Same Drag Move Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                Task.Delay(dragDelay).GetAwaiter().GetResult();
            }
            mousePosition = new Point2D()
            {
                X = Math.Abs(model.ProcessInfo.Position.Left + arrive.X * -1),
                Y = Math.Abs(model.ProcessInfo.Position.Top + arrive.Y * -1)
            };
            NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
            Task.Delay(10).GetAwaiter().GetResult();
            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
            LogHelper.Debug($">>>>Same Drag End Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
        }
        private List<Point2D> GetIntevalDragMiddlePoint(Point2D start, Point2D arrive, int interval)
        {
            List<Point2D> middlePosition = new List<Point2D>();

            Point2D recent = new Point2D(start.X, start.Y);
            middlePosition.Add(recent);

            while (recent.Subtract(arrive).Length() > interval)
            {
                LogHelper.Debug($">>> Get Middle Interval Drag Mouse : {recent.Subtract(arrive).Length()}");
                int middleX;
                if (recent.X > arrive.X)
                {
                    middleX = recent.X - interval;
                }
                else if (recent.X < arrive.X)
                {
                    middleX = recent.X + interval;
                }
                else
                {
                    middleX = recent.X;
                }

                int middleY;
                if (recent.Y > arrive.Y)
                {
                    middleY = recent.Y - interval;
                }
                else if (recent.Y < arrive.Y)
                {
                    middleY = recent.Y + interval;
                }
                else
                {
                    middleY = recent.Y;
                }

                recent = new Point2D(middleX, middleY);
                middlePosition.Add(recent);
            }

            return middlePosition;
        }
        private void ProcessMouseEvent(IntPtr hWnd, Point location, EventInfoModel model, int dragDelay)
        {
            var mousePosition = new Point2D(Math.Abs(model.ProcessInfo.Position.Left + (model.MouseEventInfo.MousePoint.X + location.X) * -1),
                Math.Abs(model.ProcessInfo.Position.Top + (model.MouseEventInfo.MousePoint.Y + location.Y) * -1));

            if (model.MouseEventInfo.MouseEventType == MouseEventType.LeftClick)
            {
                LogHelper.Debug($">>>>LMouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");

                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetAwaiter().GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.RightClick)
            {
                LogHelper.Debug($">>>>RMouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.RButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetAwaiter().GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.RButtonDown, 0, mousePosition.ToLParam());
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.Drag)
            {
                LogHelper.Debug($">>>>Drag Mouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetAwaiter().GetResult();
                for (int i = 0; i < model.MouseEventInfo.MousePoints.Count; ++i)
                {
                    var x = Math.Abs(model.ProcessInfo.Position.Left + model.MouseEventInfo.MousePoints[i].X * -1);
                    var y = Math.Abs(model.ProcessInfo.Position.Top + model.MouseEventInfo.MousePoints[i].Y * -1);
                    mousePosition = new Point2D(x, y);

                    NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                    Task.Delay(dragDelay).GetAwaiter().GetResult();
                }

                NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                Task.Delay(10).GetAwaiter().GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
                LogHelper.Debug($">>>>Drag Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
            }
        }

        public void ProcessKeyboardEvent(IntPtr hWnd, EventInfoModel model)
        {
            var hWndActive = NativeHelper.GetForegroundWindow();
            NativeHelper.SetForegroundWindow(hWnd);
            var inputs = model.KeyboardCmd.ToUpper().Split(new char[] { '+' }, StringSplitOptions.RemoveEmptyEntries);
            var modifiedKey = inputs.Where(r =>
            {
                if (Enum.TryParse($"{r}", out KeyCode keyCode))
                    return keyCode.IsExtendedKey();
                return false;
            }).Select(r =>
            {
                Enum.TryParse($"{r}", out KeyCode keyCode);
                return keyCode;
            }).ToArray();

            var command = new List<char>();
            foreach (var input in inputs)
            {
                if (Enum.TryParse(input, out KeyCode keyCode))
                {
                    if (!keyCode.IsExtendedKey())
                    {
                        for (int i = 0; i < input.Count(); i++)
                        {
                            command.Add(input[i]);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < input.Count(); i++)
                    {
                        command.Add(input[i]);
                    }
                }
            }
            var keys = command.Where(r =>
            {
                if (Enum.TryParse($"KEY_{r}", out KeyCode keyCode))
                    return !keyCode.IsExtendedKey();
                return false;
            }).Select(r =>
            {
                Enum.TryParse($"KEY_{r}", out KeyCode keyCode);
                return keyCode;
            }).ToArray();

            Keyboard.ModifiedKeyStroke(modifiedKey, keys);
            LogHelper.Debug($">>>>Keyboard Event");
            NativeHelper.SetForegroundWindow(hWndActive);
        }
    }
}
