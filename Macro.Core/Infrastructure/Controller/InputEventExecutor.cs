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
        private readonly IDesktopPlatform _desktopPlatform;
        public IKeyboardInput Keyboard { get; private set; }
        public IMouseInput Mouse { get; private set; }
        public InputEventExecutor(IKeyboardInput keyboardInput, IMouseInput mouse, IDesktopPlatform desktopPlatform)
        {
            Keyboard = keyboardInput;
            Mouse = mouse;
            _randomGenerator = new Random();
            _desktopPlatform = desktopPlatform;
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
            _desktopPlatform.TryGetWindowBounds(hWnd, out IntRect processLocation);

            var currentProcessLocation = model.ProcessInfo.Position - processLocation;

            bool physicalInput = model.HardClick;
            if (_desktopPlatform.SupportsBackgroundInput == false)
            {
                physicalInput = true;
            }
            if (physicalInput == false)
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
                if (model.MouseEventInfo.MouseEventType == MouseEventType.Drag)
                {
                    if (_desktopPlatform.SupportsBackgroundInput == false)
                    {
                        List<Point2D> points = model.MouseEventInfo.MousePoints.Select(point => new Point2D(point.X - currentProcessLocation.Left, point.Y - currentProcessLocation.Top)).ToList();
                        ProcessPhysicalDrag(hWnd, clickPoint, points, dragDelay, cancellationToken);
                        return;
                    }
                    ProcessHardClick(clickPoint, model.MouseEventInfo.MouseEventType, cancellationToken);
                }
                else
                {
                    PreparePhysicalWindow(hWnd, cancellationToken);
                    if (_desktopPlatform.SupportsBackgroundInput == false)
                    {
                        EnsurePhysicalPointMatchesWindow(hWnd, clickPoint);
                    }
                    ProcessHardClick(clickPoint, model.MouseEventInfo.MouseEventType, cancellationToken);
                }
            }
        }

        public void ProcessRelativeToImageEvent(IntPtr hWnd, EventInfoModel model, Point2D matchedLocation, ApplicationTemplate applicationTemplate, Size capturedImageSize, CancellationToken cancellationToken)
        {
            bool physicalInput = model.HardClick;
            if (_desktopPlatform.SupportsBackgroundInput == false)
            {
                physicalInput = true;
            }
            if (physicalInput == true)
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
            bool physicalInput = model.HardClick;
            if (_desktopPlatform.SupportsBackgroundInput == false)
            {
                physicalInput = true;
            }
            if (physicalInput == true)
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
            PreparePhysicalWindow(windowHandle, cancellationToken);
            IntPtr rootWindowHandle = _desktopPlatform.GetRootWindow(windowHandle);
            if (rootWindowHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The target window is unavailable.");
            }
            if (_desktopPlatform.IsMinimized(rootWindowHandle) == true)
            {
                throw new InvalidOperationException("The target window is minimized.");
            }
            if (_desktopPlatform.TryGetWindowBounds(rootWindowHandle, out IntRect windowRectangle) == false)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "The target window bounds are unavailable.");
            }
            Point clickPoint = new(windowRectangle.Left + (int)Math.Round((double)imageLocation.X * windowRectangle.Width / capturedImageSize.Width), windowRectangle.Top + (int)Math.Round((double)imageLocation.Y * windowRectangle.Height / capturedImageSize.Height));
            IntPtr clickWindowHandle = _desktopPlatform.WindowFromPoint(clickPoint);
            IntPtr clickRootWindowHandle = _desktopPlatform.GetRootWindow(clickWindowHandle);
            if (clickRootWindowHandle != rootWindowHandle)
            {
                throw new InvalidOperationException("The target click point is covered by another window.");
            }
            ProcessHardClick(clickPoint, MouseEventType.LeftClick, cancellationToken);
        }

        private void PreparePhysicalWindow(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_desktopPlatform.SupportsBackgroundInput == true)
            {
                return;
            }
            if (_desktopPlatform.HasInputPermission == false)
            {
                throw new UnauthorizedAccessException("Allow input control for Diadem Macro from Settings.");
            }
            if (_desktopPlatform.GetForegroundWindow() == windowHandle)
            {
                return;
            }
            if (_desktopPlatform.ActivateWindow(windowHandle) == false)
            {
                throw new InvalidOperationException("The target window could not be activated.");
            }
            TaskHelper.TokenCheckDelay(100, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }

        private void EnsurePhysicalPointMatchesWindow(IntPtr windowHandle, Point point)
        {
            IntPtr rootWindowHandle = _desktopPlatform.GetRootWindow(windowHandle);
            if (rootWindowHandle == IntPtr.Zero)
            {
                throw new InvalidOperationException("The target window is unavailable.");
            }
            IntPtr pointWindowHandle = _desktopPlatform.WindowFromPoint(point);
            if (_desktopPlatform.GetRootWindow(pointWindowHandle) != rootWindowHandle)
            {
                throw new InvalidOperationException("The target input point is covered by another window.");
            }
        }

        private Point2D ToPhysicalPoint(IntPtr windowHandle, Point2D imageLocation, Size capturedImageSize)
        {
            if (_desktopPlatform.TryGetWindowBounds(windowHandle, out IntRect windowRectangle) == false)
            {
                throw new InvalidOperationException("The target window bounds are unavailable.");
            }
            return new Point2D(windowRectangle.Left + (int)Math.Round((double)imageLocation.X * windowRectangle.Width / capturedImageSize.Width), windowRectangle.Top + (int)Math.Round((double)imageLocation.Y * windowRectangle.Height / capturedImageSize.Height));
        }

        private void ProcessPhysicalDrag(IntPtr windowHandle, Point2D startPoint, IList<Point2D> points, int dragDelay, CancellationToken cancellationToken)
        {
            PreparePhysicalWindow(windowHandle, cancellationToken);
            EnsurePhysicalPointMatchesWindow(windowHandle, startPoint);
            Point originalPosition = _desktopPlatform.GetCursorPosition();
            try
            {
                if (_desktopPlatform.MoveCursor(startPoint) == false)
                {
                    throw new InvalidOperationException("The cursor could not be moved to the drag position.");
                }
                Mouse.LeftButtonDown();
                try
                {
                    foreach (Point2D point in points)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        EnsurePhysicalPointMatchesWindow(windowHandle, point);
                        Point currentPosition = _desktopPlatform.GetCursorPosition();
                        Mouse.MoveMouseBy(point.X - currentPosition.X, point.Y - currentPosition.Y);
                        TaskHelper.TokenCheckDelay(dragDelay, cancellationToken);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                }
                finally
                {
                    Mouse.LeftButtonUp();
                }
            }
            finally
            {
                if (_desktopPlatform.MoveCursor(originalPosition) == false)
                {
                    LogHelper.Error("The cursor position could not be restored.");
                }
            }
        }

        private void ProcessImageEvent(IntPtr hWnd, Point2D location, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
            LogHelper.Debug($">>>>Image Location X : {location.X} Location Y : {location.Y}");

            _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonDown, 1, location.ToLParam());
            try
            {
                TaskHelper.TokenCheckDelay(10, cancellationToken);
            }
            finally
            {
                _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonUp, 0, location.ToLParam());
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
            Point currentPosition = _desktopPlatform.GetCursorPosition();
            if (_desktopPlatform.MoveCursor(clickPoint) == false)
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
                if (_desktopPlatform.MoveCursor(currentPosition) == false)
                {
                    LogHelper.Error(new Win32Exception(Marshal.GetLastWin32Error(), "The cursor position could not be restored."));
                }
            }
        }
        public void ProcessSameImageMouseDragEvent(IntPtr hWnd, Point2D start, Point2D arrive, EventInfoModel model, Size capturedImageSize, int dragDelay, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested == true)
            {
                return;
            }
            if (_desktopPlatform.SupportsBackgroundInput == false)
            {
                Point2D physicalStart = ToPhysicalPoint(hWnd, start, capturedImageSize);
                Point2D physicalArrival = ToPhysicalPoint(hWnd, arrive, capturedImageSize);
                List<Point2D> physicalPoints = GetIntervalDragMiddlePoint(physicalStart, physicalArrival, 3);
                physicalPoints.Add(physicalArrival);
                ProcessPhysicalDrag(hWnd, physicalStart, physicalPoints, dragDelay, cancellationToken);
                return;
            }
            LogHelper.Debug($">>>>Same Drag Mouse Start Target X : {arrive.X} Target Y : {arrive.Y}");
            int interval = 3;
            List<Point2D> middlePoints = GetIntervalDragMiddlePoint(start, arrive, interval);

            _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonDown, 1, start.ToLParam());
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
                    _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
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
                _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                TaskHelper.TokenCheckDelay(10, cancellationToken);
            }
            finally
            {
                _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
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

                _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                try
                {
                    TaskHelper.TokenCheckDelay(10, cancellationToken);
                }
                finally
                {
                    _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
                }
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.RightClick)
            {
                LogHelper.Debug($">>>>RMouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.RButtonDown, 1, mousePosition.ToLParam());
                try
                {
                    TaskHelper.TokenCheckDelay(10, cancellationToken);
                }
                finally
                {
                    _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.RButtonUp, 0, mousePosition.ToLParam());
                }
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.Drag)
            {
                LogHelper.Debug($">>>>Drag Mouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
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

                        _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                        if (TaskHelper.TokenCheckDelay(dragDelay, cancellationToken) == false)
                        {
                            return;
                        }
                    }

                    _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                    TaskHelper.TokenCheckDelay(10, cancellationToken);
                }
                finally
                {
                    _desktopPlatform.PostMouseMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
                }
                LogHelper.Debug($">>>>Drag Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
            }
        }

        public void ProcessKeyboardEvent(IntPtr hWnd, EventInfoModel model)
        {
            if (_desktopPlatform.SupportsBackgroundInput == false)
            {
                ProcessPhysicalKeyboardEvent(hWnd, model);
                return;
            }
            var hWndActive = _desktopPlatform.GetForegroundWindow();
            _desktopPlatform.ActivateWindow(hWnd);
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
            _desktopPlatform.ActivateWindow(hWndActive);
        }

        private void ProcessPhysicalKeyboardEvent(IntPtr windowHandle, EventInfoModel model)
        {
            IntPtr originalWindowHandle = _desktopPlatform.GetForegroundWindow();
            try
            {
                PreparePhysicalWindow(windowHandle, CancellationToken.None);
                if (_desktopPlatform.GetForegroundWindow() != windowHandle)
                {
                    throw new InvalidOperationException("The target window is not in the foreground.");
                }
                List<KeyCode> modifierKeys = new();
                List<KeyCode> commandKeys = new();
                foreach (string input in model.KeyboardCmd.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    bool isNamedKey = Enum.TryParse(input, true, out KeyCode keyCode);
                    if (char.IsDigit(input[0]) == true)
                    {
                        isNamedKey = false;
                    }
                    if (isNamedKey == true)
                    {
                        switch ((ushort)keyCode)
                        {
                            case 0x10:
                            case 0x11:
                            case 0x12:
                            case 0x5B:
                            case 0x5C:
                            case 0xA0:
                            case 0xA1:
                            case 0xA2:
                            case 0xA3:
                            case 0xA4:
                            case 0xA5:
                                modifierKeys.Add(keyCode);
                                break;
                            default:
                                commandKeys.Add(keyCode);
                                break;
                        }
                        continue;
                    }
                    foreach (char character in input.ToUpperInvariant())
                    {
                        if (Enum.TryParse("KEY_" + character, out KeyCode characterKey) == false)
                        {
                            throw new NotSupportedException("The keyboard command contains an unsupported character: " + character);
                        }
                        commandKeys.Add(characterKey);
                    }
                }
                Keyboard.ModifiedKeyStroke(modifierKeys, commandKeys);
                LogHelper.Debug(">>>>Keyboard Event");
            }
            finally
            {
                if (originalWindowHandle != IntPtr.Zero)
                {
                    _desktopPlatform.ActivateWindow(originalWindowHandle);
                }
            }
        }
    }
}
