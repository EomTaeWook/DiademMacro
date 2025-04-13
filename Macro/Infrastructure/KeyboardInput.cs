using Macro.Infrastructure.Interface;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Utils;
using Utils.Infrastructure;

namespace Macro.Infrastructure
{
    public class KeyboardInput : IKeyboardInput
    {
        public IKeyboardInput ModifiedKeyStroke(KeyCode modifierKeyCodes, KeyCode keyCodes)
        {
            return ModifiedKeyStroke(new KeyCode[] { modifierKeyCodes }, new KeyCode[] { keyCodes });
        }
        public IKeyboardInput ModifiedKeyStroke(IEnumerable<KeyCode> modifierKeyCodes, IEnumerable<KeyCode> keyCodes)
        {
            var builder = new InputBuilder();
            KeyDown(builder, modifierKeyCodes);
            KeyPress(builder, keyCodes);
            KeyUp(builder, modifierKeyCodes);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IKeyboardInput KeyUp(IEnumerable<KeyCode> keyCodes)
        {
            var builder = new InputBuilder();
            KeyUp(builder, keyCodes);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IKeyboardInput KeyDown(IEnumerable<KeyCode> keyCodes)
        {
            var builder = new InputBuilder();
            KeyDown(builder, keyCodes);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IKeyboardInput KeyPress(IEnumerable<KeyCode> keyCodes)
        {
            var builder = new InputBuilder();
            KeyPress(builder, keyCodes);
            NativeHelper.SendInput((uint)builder.Count(), builder.ToArray(), Marshal.SizeOf(typeof(Input)));
            return this;
        }

        public IKeyboardInput KeyDown(InputBuilder builder, IEnumerable<KeyCode> keyCodes)
        {
            if (keyCodes == null)
            {
                return this;
            }

            foreach (var key in keyCodes)
            {
                builder.AddKeyDown(key);
            }

            return this;
        }

        private void KeyPress(InputBuilder builder, IEnumerable<KeyCode> keyCodes)
        {
            if (keyCodes == null)
            {
                return;
            }

            foreach (var key in keyCodes)
            {
                builder.AddKeyPress(key);
            }
        }
        private void KeyUp(InputBuilder builder, IEnumerable<KeyCode> keyCodes)
        {
            if (keyCodes == null)
            {
                return;
            }

            foreach (var key in keyCodes)
            {
                builder.AddKeyUp(key);
            }
        }
    }
}
