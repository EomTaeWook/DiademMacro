using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Interface;
using System;
using System.Drawing;
using Utils.Platforms.MacOS;

namespace Macro.Infrastructure.Platforms.MacOS
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient, typeof(IMouseInput))]
    public class MacOSMouseInput : IMouseInput
    {
        private bool _leftButtonDown;
        private bool _rightButtonDown;
        private IMouseInput Send(int eventType, int button, long clickCount = 1)
        {
            Point position = MacOSNative.GetCursorPosition();
            if (MacOSNative.Mouse(eventType, button, position.X, position.Y, clickCount) == 0)
            {
                throw new UnauthorizedAccessException("Allow input control for Diadem Macro from Settings.");
            }
            return this;
        }
        public IMouseInput LeftButtonDown()
        {
            Send(1, 0);
            _leftButtonDown = true;
            return this;
        }
        public IMouseInput LeftButtonUp()
        {
            try
            {
                return Send(2, 0);
            }
            finally
            {
                _leftButtonDown = false;
            }
        }
        public IMouseInput LeftButtonClick()
        {
            LeftButtonDown();
            return LeftButtonUp();
        }
        public IMouseInput LeftButtonDoubleClick()
        {
            LeftButtonClick();
            Send(1, 0, 2);
            return Send(2, 0, 2);
        }
        public IMouseInput RightButtonDown()
        {
            Send(3, 1);
            _rightButtonDown = true;
            return this;
        }
        public IMouseInput RightButtonUp()
        {
            try
            {
                return Send(4, 1);
            }
            finally
            {
                _rightButtonDown = false;
            }
        }
        public IMouseInput RightButtonClick()
        {
            RightButtonDown();
            return RightButtonUp();
        }
        public IMouseInput RightButtonDoubleClick()
        {
            RightButtonClick();
            Send(3, 1, 2);
            return Send(4, 1, 2);
        }
        public IMouseInput MoveMouseBy(int pixelX, int pixelY)
        {
            Point position = MacOSNative.GetCursorPosition();
            return MoveToPoint(position.X + pixelX, position.Y + pixelY);
        }
        public IMouseInput MoveMouseTo(int absoluteX, int absoluteY)
        {
            return MoveMouseToVirtualDesktop(absoluteX, absoluteY);
        }
        public IMouseInput MoveMouseToVirtualDesktop(int absoluteX, int absoluteY)
        {
            var monitors = MacOSNative.GetMonitors();
            if (monitors.Count == 0)
            {
                throw new InvalidOperationException("No display is available.");
            }
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;
            foreach (var monitor in monitors)
            {
                left = Math.Min(left, monitor.Rect.Left);
                top = Math.Min(top, monitor.Rect.Top);
                right = Math.Max(right, monitor.Rect.Right);
                bottom = Math.Max(bottom, monitor.Rect.Bottom);
            }
            return MoveToPoint(left + Math.Clamp(absoluteX, 0, 65535) * (right - left - 1) / 65535.0, top + Math.Clamp(absoluteY, 0, 65535) * (bottom - top - 1) / 65535.0);
        }
        private IMouseInput MoveToPoint(double x, double y)
        {
            int eventType = 5;
            int button = 0;
            if (_leftButtonDown == true)
            {
                eventType = 6;
            }
            if (_rightButtonDown == true)
            {
                eventType = 7;
                button = 1;
            }
            if (MacOSNative.Mouse(eventType, button, x, y, 0) == 0)
            {
                throw new UnauthorizedAccessException("Allow input control for Diadem Macro from Settings.");
            }
            return this;
        }
    }
}
