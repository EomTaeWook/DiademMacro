using DataContainer.Generated;
using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Dignus.Utils;
using Dignus.Utils.Extensions;
using Macro.Extensions;
using Macro.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Utils;
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
            EventTriggerModel model,
            Point matchedLocation,
            ApplicationTemplate applicationTemplate,
            int dragDelay)
        {
            var processLocation = new Utils.Infrastructure.Rect();
            NativeHelper.GetWindowRect(hWnd, ref processLocation);

            var currentProcessLocation = model.ProcessInfo.Position -
processLocation;


            if (model.HardClick == false)
            {
                matchedLocation.X = applicationTemplate.OffsetX;
                matchedLocation.Y = applicationTemplate.OffsetY;
                MouseTriggerProcess(hWnd, matchedLocation, model, dragDelay);
            }
            else
            {
                var clickPoint = new Point
                {
                    X = model.MouseTriggerInfo.StartPoint.X - currentProcessLocation.Left,
                    Y = model.MouseTriggerInfo.StartPoint.Y - currentProcessLocation.Top
                };
                HardClickProcess(clickPoint, model.MouseTriggerInfo.MouseInfoEventType);
            }
        }

        public void HandleRelativeToImageEvent(IntPtr hWnd,
            EventTriggerModel model,
            Point matchedLocation,
            ApplicationTemplate applicationTemplate)
        {
            matchedLocation.X = (matchedLocation.X + applicationTemplate.OffsetX) + (model.Image.Width / 2);
            matchedLocation.Y = (matchedLocation.Y + applicationTemplate.OffsetY) + (model.Image.Height / 2);

            ImageTriggerProcess(hWnd,
                matchedLocation,
                model);
        }

        public void HandleImageEvent(IntPtr hWnd,
            EventTriggerModel model,
            Point matchedLocation,
            ApplicationTemplate applicationTemplate)
        {
            var percentageX = _randomGenerator.NextDouble();
            var percentageY = _randomGenerator.NextDouble();

            matchedLocation.X = (matchedLocation.X + applicationTemplate.OffsetX) + (model.Image.Width * percentageX);
            matchedLocation.Y = (matchedLocation.Y + applicationTemplate.OffsetY) + (model.Image.Height * percentageY);

            if (model.HardClick == false)
            {
                ImageTriggerProcess(hWnd, matchedLocation, model);
            }
            else
            {
                var processLocation = new Utils.Infrastructure.Rect();
                NativeHelper.GetWindowRect(hWnd, ref processLocation);

                var clickPoint = new Point()
                {
                    X = matchedLocation.X,
                    Y = matchedLocation.Y
                };
                clickPoint.X += processLocation.Left + model.MouseTriggerInfo.StartPoint.X;
                clickPoint.Y += processLocation.Top + model.MouseTriggerInfo.StartPoint.Y;
                HardClickProcess(clickPoint, MouseEventType.LeftClick);
            }
        }

        private void ImageTriggerProcess(IntPtr hWnd,
                                        Point location,
                                        EventTriggerModel model)
        {
            var position = new Point()
            {
                X = location.X + model.MouseTriggerInfo.StartPoint.X,
                Y = location.Y + model.MouseTriggerInfo.StartPoint.Y
            };

            LogHelper.Debug($">>>>Image Location X : {position.X} Location Y : {position.Y}");

            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, position.ToLParam());
            Task.Delay(10).GetResult();
            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, position.ToLParam());
        }
        public void HardClickProcess(Point clickPoint, MouseEventType mouseEventType)
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
        public void SameImageMouseDragTriggerProcess(IntPtr hWnd,
                                            Point start,
                                            Point arrive,
                                            EventTriggerModel model,
                                            int dragDelay)
        {
            LogHelper.Debug($">>>>Same Drag Mouse Start Target X : {arrive.X} Target Y : {arrive.Y}");
            var interval = 3;
            var middlePoints = this.GetIntevalDragMiddlePoint(start, arrive, interval);

            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, start.ToLParam());
            Task.Delay(10).GetResult();

            Point mousePosition;
            for (int i = 0; i < middlePoints.Count; ++i)
            {
                mousePosition = new Point()
                {
                    X = Math.Abs(model.ProcessInfo.Position.Left + middlePoints[i].X * -1),
                    Y = Math.Abs(model.ProcessInfo.Position.Top + middlePoints[i].Y * -1)
                };
                LogHelper.Debug($">>>>Same Drag Move Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                Task.Delay(dragDelay).GetResult();
            }
            mousePosition = new Point()
            {
                X = Math.Abs(model.ProcessInfo.Position.Left + arrive.X * -1),
                Y = Math.Abs(model.ProcessInfo.Position.Top + arrive.Y * -1)
            };
            NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
            Task.Delay(10).GetResult();
            NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
            LogHelper.Debug($">>>>Same Drag End Mouse Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
        }
        private List<Point> GetIntevalDragMiddlePoint(Point start, Point arrive, int interval)
        {
            List<Point> middlePosition = new List<Point>();

            Point recent = new Point(start.X, start.Y);
            middlePosition.Add(recent);

            while (Point.Subtract(recent, arrive).Length > interval)
            {
                LogHelper.Debug($">>> Get Middle Interval Drag Mouse : {Point.Subtract(recent, arrive).Length}");
                double middleX;
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

                double middleY;
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

                recent = new Point(middleX, middleY);
                middlePosition.Add(recent);
            }

            return middlePosition;
        }
        private void MouseTriggerProcess(IntPtr hWnd, Point location, EventTriggerModel model, int dragDelay)
        {
            var mousePosition = new Point()
            {
                X = Math.Abs(model.ProcessInfo.Position.Left + (model.MouseTriggerInfo.StartPoint.X + location.X) * -1),
                Y = Math.Abs(model.ProcessInfo.Position.Top + (model.MouseTriggerInfo.StartPoint.Y + location.Y) * -1)
            };

            if (model.MouseTriggerInfo.MouseInfoEventType == MouseEventType.LeftClick)
            {
                LogHelper.Debug($">>>>LMouse Save Position X : {model.MouseTriggerInfo.StartPoint.X} Save Position Y : {model.MouseTriggerInfo.StartPoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");

                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
            }
            else if (model.MouseTriggerInfo.MouseInfoEventType == MouseEventType.RightClick)
            {
                LogHelper.Debug($">>>>RMouse Save Position X : {model.MouseTriggerInfo.StartPoint.X} Save Position Y : {model.MouseTriggerInfo.StartPoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.RButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.RButtonDown, 0, mousePosition.ToLParam());
            }
            else if (model.MouseTriggerInfo.MouseInfoEventType == MouseEventType.Drag)
            {
                LogHelper.Debug($">>>>Drag Mouse Save Position X : {model.MouseTriggerInfo.StartPoint.X} Save Position Y : {model.MouseTriggerInfo.StartPoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                Task.Delay(10).GetResult();
                for (int i = 0; i < model.MouseTriggerInfo.MiddlePoint.Count; ++i)
                {
                    mousePosition = new Point()
                    {
                        X = Math.Abs(model.ProcessInfo.Position.Left + model.MouseTriggerInfo.MiddlePoint[i].X * -1),
                        Y = Math.Abs(model.ProcessInfo.Position.Top + model.MouseTriggerInfo.MiddlePoint[i].Y * -1)
                    };
                    NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                    Task.Delay(dragDelay).GetResult();
                }
                mousePosition = new Point()
                {
                    X = Math.Abs(model.ProcessInfo.Position.Left + model.MouseTriggerInfo.EndPoint.X * -1),
                    Y = Math.Abs(model.ProcessInfo.Position.Top + model.MouseTriggerInfo.EndPoint.Y * -1)
                };
                NativeHelper.PostMessage(hWnd, WindowMessage.MouseMove, 1, mousePosition.ToLParam());
                Task.Delay(10).GetResult();
                NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
                LogHelper.Debug($">>>>Drag Mouse Save Position X : {model.MouseTriggerInfo.EndPoint.X} Save Position Y : {model.MouseTriggerInfo.EndPoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");
            }
            else if (model.MouseTriggerInfo.MouseInfoEventType == MouseEventType.Wheel)
            {
                LogHelper.Debug($">>>>Wheel Save Position X : {model.MouseTriggerInfo.StartPoint.X} Save Position Y : {model.MouseTriggerInfo.StartPoint.Y} Target X : {mousePosition.X} Target Y : {mousePosition.Y}");

                //NativeHelper.PostMessage(hWnd, WindowMessage.LButtonDown, 1, mousePosition.ToLParam());
                //Task.Delay(100).Wait();
                //NativeHelper.PostMessage(hWnd, WindowMessage.LButtonUp, 0, mousePosition.ToLParam());
                //NativeHelper.PostMessage(hWnd, WindowMessage.MouseWheel, ObjectExtensions.MakeWParam((uint)WindowMessage.MKControl, (uint)(model.MouseTriggerInfo.WheelData * -1)), 0);
                //var hwnd = NativeHelper.FindWindowEx(NativeHelper.FindWindow(null, "Test.txt - 메모장"), IntPtr.Zero, "Edit", null);
                //var p = new System.Drawing.Point(0, 0);
                NativeHelper.PostMessage(hWnd, WindowMessage.MouseWheel, ObjectExtensions.MakeWParam(0, model.MouseTriggerInfo.WheelData * ConstHelper.WheelDelta), mousePosition.ToLParam());
            }
        }

        public void KeyboardTriggerProcess(IntPtr hWnd, EventTriggerModel model)
        {
            var hWndActive = NativeHelper.GetForegroundWindow();
            Task.Delay(10).GetResult();

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
            Task.Delay(10).GetResult();
            LogHelper.Debug($">>>>Keyboard Event");
            NativeHelper.SetForegroundWindow(hWndActive);
        }
    }
}
