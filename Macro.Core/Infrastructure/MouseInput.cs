using Macro.Infrastructure.Interface;
using System.Linq;
using Dignus.DependencyInjection.Attributes;
using System.Runtime.InteropServices;
using Utils;
using Utils.Infrastructure;

namespace Macro.Infrastructure
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient, typeof(IMouseInput))]
    public class MouseInput : IMouseInput
    {
        public IMouseInput LeftButtonClick()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonClick(MouseButton.LeftButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IMouseInput LeftButtonDoubleClick()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonDoubleClick(MouseButton.LeftButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IMouseInput LeftButtonUp()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonUp(MouseButton.LeftButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }
        public IMouseInput LeftButtonDown()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonDown(MouseButton.LeftButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IMouseInput MoveMouseBy(int pixelX, int pixelY)
        {
            var builder = new InputBuilder();
            builder.AddRelativeMouseMove(pixelX, pixelY);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IMouseInput MoveMouseTo(int absoluteX, int absoluteY)
        {
            var builder = new InputBuilder();
            builder.AddAbsoluteMouseMove(absoluteX, absoluteY);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }
        public IMouseInput MoveMouseToVirtualDesktop(int absoluteX, int absoluteY)
        {
            var builder = new InputBuilder();
            builder.AddAbsoluteMouseMoveOnVirtualDesktop(absoluteX, absoluteY);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }
        public IMouseInput RightButtonClick()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonUp(MouseButton.RightButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IMouseInput RightButtonDoubleClick()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonDoubleClick(MouseButton.RightButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IMouseInput RightButtonUp()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonUp(MouseButton.RightButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }
        public IMouseInput RightButtonDown()
        {
            var builder = new InputBuilder();
            builder.AddMouseButtonDown(MouseButton.RightButton);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }
    }
}
