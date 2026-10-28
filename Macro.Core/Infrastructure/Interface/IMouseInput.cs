namespace Macro.Infrastructure.Interface
{
    public interface IMouseInput
    {
        IMouseInput MoveMouseBy(int pixelX, int pixelY);
        IMouseInput MoveMouseTo(int absoluteX, int absoluteY);
        IMouseInput MoveMouseToVirtualDesktop(int absoluteX, int absoluteY);

        IMouseInput LeftButtonDown();
        IMouseInput LeftButtonUp();
        IMouseInput LeftButtonClick();
        IMouseInput LeftButtonDoubleClick();

        IMouseInput RightButtonDown();
        IMouseInput RightButtonUp();
        IMouseInput RightButtonClick();
        IMouseInput RightButtonDoubleClick();
    }
}
