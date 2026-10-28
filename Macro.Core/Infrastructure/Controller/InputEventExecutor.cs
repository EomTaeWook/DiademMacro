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
using System.Threading;
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
        public void ProcessMouseEvent(IntPtr hWnd, EventInfoModel model, Point2D matchedLocation, ApplicationTemplate applicationTemplate, int dragDelay, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
            var processLocation = new IntRect();
            NativeHelper.GetWindowRect(hWnd, ref processLocation);

            var currentProcessLocation = model.ProcessInfo.Position - processLocation;

            if (model.HardClick == false)
            {
                matchedLocation.X = applicationTemplate.OffsetX;
                matchedLocation.Y = applicationTemplate.OffsetY;
                ProcessMouseEvent(hWnd, matchedLocation, model, dragDelay, cancellationToken);
            }
            else
            {
                var clickPoint = new Point2D
                {
                    X = model.MouseEventInfo.MousePoint.X - currentProcessLocation.Left,
                    Y = model.MouseEventInfo.MousePoint.Y - currentProcessLocation.Top
                };
                ProcessHardClick(clickPoint, model.MouseEventInfo.MouseEventType, cancellationToken);
            }
        }

        public void ProcessRelativeToImageEvent(IntPtr hWnd, EventInfoModel model, Point2D matchedLocation, ApplicationTemplate applicationTemplate, Size capturedImageSize, CancellationToken cancellationToken)
        {
            if (model.HardClick == true)
            {
                Point2D clickPoint = new(matchedLocation.X + model.Image.Width / 2 + model.PositionRelativeToImage.X, matchedLocation.Y + model.Image.Height / 2 + model.PositionRelativeToImage.Y);
                ProcessPhysicalImageClick(hWnd, clickPoint, capturedImageSize, cancellationToken);
                return;
            }
            matchedLocation.X = (matchedLocation.X + applicationTemplate.OffsetX) + (model.Image.Width / 2);
            matchedLocation.Y = (matchedLocation.Y + applicationTemplate.OffsetY) + (model.Image.Height / 2);

            matchedLocation.X += model.PositionRelativeToImage.X;
            matchedLocation.Y += model.PositionRelativeToImage.Y;

            ProcessImageEvent(hWnd, matchedLocation, cancellationToken);
        }

        public void ProcessImageEvent(IntPtr hWnd, EventInfoModel model, Point2D matchedLocation, ApplicationTemplate applicationTemplate, Size capturedImageSize, CancellationToken cancellationToken)
        {
            if (model.HardClick == true)
            {
                Point2D clickPoint = new(matchedLocation.X + model.Image.Width / 2, matchedLocation.Y + model.Image.Height / 2);
                ProcessPhysicalImageClick(hWnd, clickPoint, capturedImageSize, cancellationToken);
                return;
            }
            int offsetX = _randomGenerator.Next(0, model.Image.Width);
            int offsetY = _randomGenerator.Next(0, model.Image.Height);

            matchedLocation.X = matchedLocation.X + applicationTemplate.OffsetX + offsetX;
            matchedLocation.Y = matchedLocation.Y + applicationTemplate.OffsetY + offsetY;

            ProcessImageEvent(hWnd, matchedLocation, cancellationToken);
        }

        private void ProcessPhysicalImageClick(IntPtr windowHandle, Point2D imageLocation, Size capturedImageSize, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
            IntPtr rootWindowHandle = NativeHelper.GetAncestor(windowHandle, NativeHelper.RootAncestor);
            if (rootWindowHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The target window is unavailable.");
            }
            if (NativeHelper.IsIconic(rootWindowHandle) == true)
            {
                throw new InvalidOperationException("The target window is minimized.");
            }
            IntRect windowRectangle = new();
            if (NativeHelper.GetWindowRect(rootWindowHandle, ref windowRectangle) == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The target window bounds are unavailable.");
            }
            Point clickPoint = new(windowRectangle.Left + (int)Math.Round((double)imageLocation.X * windowRectangle.Width / capturedImageSize.Width), windowRectangle.Top + (int)Math.Round((double)imageLocation.Y * windowRectangle.Height / capturedImageSize.Height));
            IntPtr clickWindowHandle = NativeHelper.WindowFromPoint(clickPoint);
            IntPtr clickRootWindowHandle = NativeHelper.GetAncestor(clickWindowHandle, NativeHelper.RootAncestor);
            if (clickRootWindowHandle != rootWindowHandle)
            {
                throw new InvalidOperationException("The target click point is covered by another window.");
            }
            ProcessHardClick(clickPoint, MouseEventType.LeftClick, cancellationToken);
        }

        private void ProcessImageEvent(IntPtr hWnd, Point2D location, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
            LogHelper.Debug($">>>>Image Location X : {location.X} Location Y : {location.Y}");

            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, location.ToLParam());
            try
            {
                TaskHelper.TokenCheckDelay(10, cancellationToken);
            }
            finally
            {
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, location.ToLParam());
            }
        }
        public void ProcessHardClick(Point clickPoint, MouseEventType mouseEventType, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
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
                        TaskHelper.TokenCheckDelay(10, cancellationToken);
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
                        TaskHelper.TokenCheckDelay(10, cancellationToken);
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
        public void ProcessSameImageMouseDragEvent(IntPtr hWnd, Point2D start, Point2D arrive, EventInfoModel model, int dragDelay, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
            LogHelper.Debug($">>>>Same Drag Mouse Start Target X : {arrive.X} Target Y : {arrive.Y}");
            int interval = 3;
            List<Point2D> middlePoints = GetIntervalDragMiddlePoint(start, arrive, interval);

            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, start.ToLParam());
            Point2D mousePosition = start;
            try
            {
                if (TaskHelper.TokenCheckDelay(10, cancellationToken) == false)
                {
                    return;
                }
                for (int index = 0; index < middlePoints.Count; ++index)
                {
                    if (cancellationToken.IsCancellationRequested == true)
                    {
                        return;
                    }
                    mousePosition = new Point2D
                    {
                        X = Math.Abs(model.ProcessInfo.Position.Left + middlePoints[index].X * -1),
                        Y = Math.Abs(model.ProcessInfo.Position.Top + middlePoints[index].Y * -1)
                    };
                    LogHelper.Debug($">>>>Same Drag Move Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                    NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                    if (TaskHelper.TokenCheckDelay(dragDelay, cancellationToken) == false)
                    {
                        return;
                    }
                }
                mousePosition = new Point2D
                {
                    X = Math.Abs(model.ProcessInfo.Position.Left + arrive.X * -1),
                    Y = Math.Abs(model.ProcessInfo.Position.Top + arrive.Y * -1)
                };
                NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                TaskHelper.TokenCheckDelay(10, cancellationToken);
            }
            finally
            {
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
            }
            LogHelper.Debug($">>>>Same Drag End Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
        }
        private List<Point2D> GetIntervalDragMiddlePoint(Point2D start, Point2D arrive, int interval)
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
        private void ProcessMouseEvent(IntPtr hWnd, Point location, EventInfoModel model, int dragDelay, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
            var mousePosition = new Point2D(Math.Abs(model.ProcessInfo.Position.Left + (model.MouseEventInfo.MousePoint.X + location.X) * -1),
                Math.Abs(model.ProcessInfo.Position.Top + (model.MouseEventInfo.MousePoint.Y + location.Y) * -1));

            if (model.MouseEventInfo.MouseEventType == MouseEventType.LeftClick)
            {
                LogHelper.Debug($">>>>LMouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");

                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                try
                {
                    TaskHelper.TokenCheckDelay(10, cancellationToken);
                }
                finally
                {
                    NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
                }
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.RightClick)
            {
                LogHelper.Debug($">>>>RMouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.RButtonDown, 1, mousePosition.ToLParam());
                try
                {
                    TaskHelper.TokenCheckDelay(10, cancellationToken);
                }
                finally
                {
                    NativeHelper.PostMessage(hWnd, WindowMessage.RButtonUp, 0, mousePosition.ToLParam());
                }
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.Drag)
            {
                LogHelper.Debug($">>>>Drag Mouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                try
                {
                    if (TaskHelper.TokenCheckDelay(10, cancellationToken) == false)
                    {
                        return;
                    }
                    for (int index = 0; index < model.MouseEventInfo.MousePoints.Count; ++index)
                    {
                        if (cancellationToken.IsCancellationRequested == true)
                        {
                            return;
                        }
                        int x = Math.Abs(model.ProcessInfo.Position.Left + model.MouseEventInfo.MousePoints[index].X * -1);
                        int y = Math.Abs(model.ProcessInfo.Position.Top + model.MouseEventInfo.MousePoints[index].Y * -1);
                        mousePosition = new Point2D(x, y);

                        NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                        if (TaskHelper.TokenCheckDelay(dragDelay, cancellationToken) == false)
                        {
                            return;
                        }
                    }

                    NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                    TaskHelper.TokenCheckDelay(10, cancellationToken);
                }
                finally
                {
                    NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
                }
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
