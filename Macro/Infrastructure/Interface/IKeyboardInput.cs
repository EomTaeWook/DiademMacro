using System.Collections.Generic;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Interface
{
    public interface IKeyboardInput
    {
        IKeyboardInput ModifiedKeyStroke(KeyCode modifierKeyCodes, KeyCode keyCodes);
        IKeyboardInput ModifiedKeyStroke(IEnumerable<KeyCode> modifierKeyCodes, IEnumerable<KeyCode> keyCodes);

        IKeyboardInput KeyUp(IEnumerable<KeyCode> keyCodes);

        IKeyboardInput KeyDown(IEnumerable<KeyCode> keyCodes);

        IKeyboardInput KeyPress(IEnumerable<KeyCode> keyCodes);
    }
}
