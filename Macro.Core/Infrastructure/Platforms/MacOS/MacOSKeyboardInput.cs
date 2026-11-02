using Dignus.DependencyInjection.Attributes;
using Dignus.Log;
using Macro.Infrastructure.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using Utils.Infrastructure;
using Utils.Platforms.MacOS;

namespace Macro.Infrastructure.Platforms.MacOS
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient, typeof(IKeyboardInput))]
    public class MacOSKeyboardInput : IKeyboardInput
    {
        private ulong _modifierFlags;
        public IKeyboardInput ModifiedKeyStroke(KeyCode modifierKeyCodes, KeyCode keyCodes)
        {
            return ModifiedKeyStroke(new[] { modifierKeyCodes }, new[] { keyCodes });
        }
        public IKeyboardInput ModifiedKeyStroke(IEnumerable<KeyCode> modifierKeyCodes, IEnumerable<KeyCode> keyCodes)
        {
            List<KeyCode> pressed = new();
            try
            {
                if (modifierKeyCodes != null)
                {
                    foreach (KeyCode modifier in modifierKeyCodes)
                    {
                        Send(modifier, true);
                        pressed.Add(modifier);
                    }
                }
                KeyPress(keyCodes);
                return this;
            }
            finally
            {
                for (int index = pressed.Count - 1; index >= 0; index--)
                {
                    try
                    {
                        Send(pressed[index], false);
                    }
                    catch (Exception exception)
                    {
                        LogHelper.Error(exception);
                    }
                }
            }
        }
        public IKeyboardInput KeyUp(IEnumerable<KeyCode> keyCodes)
        {
            if (keyCodes != null)
            {
                foreach (KeyCode key in keyCodes.Reverse())
                {
                    Send(key, false);
                }
            }
            return this;
        }
        public IKeyboardInput KeyDown(IEnumerable<KeyCode> keyCodes)
        {
            if (keyCodes != null)
            {
                foreach (KeyCode key in keyCodes)
                {
                    Send(key, true);
                }
            }
            return this;
        }
        public IKeyboardInput KeyPress(IEnumerable<KeyCode> keyCodes)
        {
            if (keyCodes != null)
            {
                foreach (KeyCode key in keyCodes)
                {
                    try
                    {
                        Send(key, true);
                    }
                    finally
                    {
                        Send(key, false);
                    }
                }
            }
            return this;
        }
        private void Send(KeyCode key, bool isDown)
        {
            ushort virtualKey = (ushort)key;
            ushort nativeKey = MapKey(virtualKey);
            ulong modifier = GetModifier(virtualKey);
            ulong previousFlags = _modifierFlags;
            if (isDown == true)
            {
                _modifierFlags |= modifier;
            }
            else
            {
                _modifierFlags &= ~modifier;
            }
            int direction = 0;
            if (isDown == true)
            {
                direction = 1;
            }
            if (MacOSNative.Key(nativeKey, direction, _modifierFlags) == 0)
            {
                _modifierFlags = previousFlags;
                throw new UnauthorizedAccessException("Allow input control for Diadem Macro from Settings.");
            }
        }
        private static ulong GetModifier(ushort key)
        {
            switch (key)
            {
                case 0x10: case 0xA0: case 0xA1: return 1UL << 17;
                case 0x11: case 0xA2: case 0xA3: return 1UL << 18;
                case 0x12: case 0xA4: case 0xA5: return 1UL << 19;
                case 0x5B: case 0x5C: return 1UL << 20;
                default: return 0;
            }
        }
        private static ushort MapKey(ushort key)
        {
            switch (key)
            {
                case 0x41: return 0; case 0x53: return 1; case 0x44: return 2; case 0x46: return 3;
                case 0x48: return 4; case 0x47: return 5; case 0x5A: return 6; case 0x58: return 7;
                case 0x43: return 8; case 0x56: return 9; case 0x42: return 11; case 0x51: return 12;
                case 0x57: return 13; case 0x45: return 14; case 0x52: return 15; case 0x59: return 16;
                case 0x54: return 17; case 0x31: return 18; case 0x32: return 19; case 0x33: return 20;
                case 0x34: return 21; case 0x36: return 22; case 0x35: return 23; case 0xBB: return 24;
                case 0x39: return 25; case 0x37: return 26; case 0xBD: return 27; case 0x38: return 28;
                case 0x30: return 29; case 0xDD: return 30; case 0x4F: return 31; case 0x55: return 32;
                case 0xDB: return 33; case 0x49: return 34; case 0x50: return 35; case 0x0D: return 36;
                case 0x4C: return 37; case 0x4A: return 38; case 0xDE: return 39; case 0x4B: return 40;
                case 0xBA: return 41; case 0xDC: return 42; case 0xBC: return 43; case 0xBF: return 44;
                case 0x4E: return 45; case 0x4D: return 46; case 0xBE: return 47; case 0x09: return 48;
                case 0x20: return 49; case 0xC0: return 50; case 0x08: return 51; case 0x1B: return 53;
                case 0x5B: return 55; case 0x10: case 0xA0: return 56; case 0x14: return 57;
                case 0x12: case 0xA4: return 58; case 0x11: case 0xA2: return 59; case 0xA1: return 60;
                case 0xA5: return 61; case 0xA3: return 62; case 0x5C: return 54;
                case 0x7B: return 111; case 0x7A: return 103; case 0x79: return 109; case 0x78: return 101;
                case 0x77: return 100; case 0x76: return 98; case 0x75: return 97; case 0x74: return 96;
                case 0x73: return 118; case 0x72: return 99; case 0x71: return 120; case 0x70: return 122;
                case 0x7C: return 105; case 0x7D: return 107; case 0x7E: return 113; case 0x7F: return 106;
                case 0x80: return 64; case 0x81: return 79; case 0x82: return 80; case 0x83: return 90;
                case 0x24: return 115; case 0x23: return 119; case 0x21: return 116; case 0x22: return 121;
                case 0x25: return 123; case 0x26: return 126; case 0x27: return 124; case 0x28: return 125;
                case 0x2E: return 117; case 0x6E: return 65; case 0x6A: return 67; case 0x6B: return 69;
                case 0x6F: return 75; case 0x6D: return 78; case 0x60: return 82; case 0x61: return 83;
                case 0x62: return 84; case 0x63: return 85; case 0x64: return 86; case 0x65: return 87;
                case 0x66: return 88; case 0x67: return 89; case 0x68: return 91; case 0x69: return 92;
                default: throw new NotSupportedException("This key has no macOS mapping: " + key);
            }
        }
    }
}
