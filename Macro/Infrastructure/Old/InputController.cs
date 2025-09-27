using Dignus.DependencyInjection.Attributes;
using Macro.Infrastructure.Interface;

namespace Macro.Infrastructure.ControllerOld
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class InputController
    {
        public IKeyboardInput Keyboard { get; private set; }
        public IMouseInput Mouse { get; private set; }

        public InputController(IKeyboardInput keyboardInput, IMouseInput mouse)
        {
            Keyboard = keyboardInput;
            Mouse = mouse;
        }
    }
}
