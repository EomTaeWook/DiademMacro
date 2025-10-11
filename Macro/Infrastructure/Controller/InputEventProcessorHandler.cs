using DataContainer.Generated;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Dignus.Utils;
using Dignus.Utils.Extensions;
using Macro.Extensions;
using Macro.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Utils;
using Utils.Extensions;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Controller
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class InputEventProcessorHandler
    {
        private readonly RandomGenerator _randomGenerator;
        private readonly InputController _inputController;
        public InputEventProcessorHandler(InputController inputController)
        {
            _inputController = inputController;
            _randomGenerator = new RandomGenerator();
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
        public void HandleMouseEvent(IntPtr hWnd,
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

        public void HandleRelativeToImageEvent(IntPtr hWnd,
            EventInfoModel model,
            Point matchedLocation,
            ApplicationTemplate applicationTemplate)
        {
            matchedLocation.X = (matchedLocation.X + applicationTemplate.OffsetX) + (model.Image.Width / 2);
            matchedLocation.Y = (matchedLocation.Y + applicationTemplate.OffsetY) + (model.Image.Height / 2);

            ProcessImageEvent(hWnd,
                matchedLocation,
                model);
        }

        public void HandleImageEvent(IntPtr hWnd,
            EventInfoModel model,
            Point2D matchedLocation,
            ApplicationTemplate applicationTemplate)
        {
            var percentageX = (int)_randomGenerator.NextDouble();
            var percentageY = (int)_randomGenerator.NextDouble();

            matchedLocation.X = (matchedLocation.X + applicationTemplate.OffsetX) + (model.Image.Width * percentageX);
            matchedLocation.Y = (matchedLocation.Y + applicationTemplate.OffsetY) + (model.Image.Height * percentageY);

            if (model.HardClick == false)
            {
                ProcessImageEvent(hWnd, matchedLocation, model);
            }
            else
            {
                var processLocation = new Utils.Infrastructure.IntRect();
                NativeHelper.GetWindowRect(hWnd, ref processLocation);

                var clickPoint = new Point2D()
                {
                    X = matchedLocation.X,
                    Y = matchedLocation.Y
                };
                clickPoint.X += processLocation.Left + model.MouseEventInfo.MousePoint.X;
                clickPoint.Y += processLocation.Top + model.MouseEventInfo.MousePoint.Y;
                ProcessHardClick(clickPoint, model.MouseEventInfo.MouseEventType);
            }
        }

        private void ProcessImageEvent(IntPtr hWnd,
                                        Point location,
                                        EventInfoModel model)
        {
            var position = new Point2D(location.X + model.MouseEventInfo.MousePoint.X,
                location.Y + model.MouseEventInfo.MousePoint.Y);

            LogHelper.Debug($">>>>Image Location X : {position.X} Location Y : {position.Y}");

            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, position.ToLParam());
            Task.Delay(10).GetResult();
            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, position.ToLParam());
        }
        public void ProcessHardClick(Point clickPoint, MouseEventType mouseEventType)
        {
            var currentPosition = NativeHelper.GetCursorPosition();

            if (mouseEventType == MouseEventType.LeftClick)
            {
                _inputController.Mouse.MoveMouseTo((int)clickPoint.X, (int)clickPoint.Y);
                _inputController.Mouse.LeftButtonDown();
                Task.Delay(10).GetResult();
                _inputController.Mouse.LeftButtonUp();
                _inputController.Mouse.MoveMouseTo((int)currentPosition.X, (int)currentPosition.Y);
            }
            else if (mouseEventType == MouseEventType.RightClick)
            {
                _inputController.Mouse.MoveMouseTo((int)clickPoint.X, (int)clickPoint.Y);
                _inputController.Mouse.RightButtonDown();
                Task.Delay(10).GetResult();
                _inputController.Mouse.RightButtonUp();
                _inputController.Mouse.MoveMouseTo((int)currentPosition.X, (int)currentPosition.Y);
            }
            else
            {
                LogHelper.Error($"unsupported MouseEventType: {mouseEventType}");
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
            Task.Delay(10).GetResult();

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
                Task.Delay(dragDelay).GetResult();
            }
            mousePosition = new Point2D()
            {
                X = Math.Abs(model.ProcessInfo.Position.Left + arrive.X * -1),
                Y = Math.Abs(model.ProcessInfo.Position.Top + arrive.Y * -1)
            };
            NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
            Task.Delay(10).GetResult();
            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
            LogHelper.Debug($">>>>Same Drag End Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
        }
        private List<Point2D> GetIntevalDragMiddlePoint(Point2D start, Point2D arrive, int interval)
        {
            List<Point2D> middlePosition = new List<Point2D>();

            Point2D recent = new Point2D(start.X, start.Y);
            middlePosition.Add(recent);

            while (recent.Subtract(arrive).Length > interval)
            {
                LogHelper.Debug($">>> Get Middle Interval Drag Mouse : {recent.Subtract(arrive).Length}");
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
                Task.Delay(10).GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.RightClick)
            {
                LogHelper.Debug($">>>>RMouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.RButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.RButtonDown, 0, mousePosition.ToLParam());
            }
            else if (model.MouseEventInfo.MouseEventType == MouseEventType.Drag)
            {
                LogHelper.Debug($">>>>Drag Mouse Save Position X : {model.MouseEventInfo.MousePoint.X} Save Position Y : {model.MouseEventInfo.MousePoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetResult();
                for (int i = 0; i < model.MouseEventInfo.MousePoints.Count; ++i)
                {
                    var x = Math.Abs(model.ProcessInfo.Position.Left + model.MouseEventInfo.MousePoints[i].X * -1);
                    var y = Math.Abs(model.ProcessInfo.Position.Top + model.MouseEventInfo.MousePoints[i].Y * -1);
                    mousePosition = new Point2D(x, y);

                    NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                    Task.Delay(dragDelay).GetResult();
                }

                NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                Task.Delay(10).GetResult();
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

            _inputController.Keyboard.ModifiedKeyStroke(modifiedKey, keys);
            LogHelper.Debug($">>>>Keyboard Event");
            NativeHelper.SetForegroundWindow(hWndActive);
        }
    }
}
