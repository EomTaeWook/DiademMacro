using System;

namespace Utils.Infrastructure
{
    [Flags]
    internal enum KeyboardFlag : uint
    {
        ExtendedKey = 0x0001,
        KeyUp = 0x0002,
        Unicode = 0x0004,
        ScanCode = 0x0008,
    }
    [Flags]
    public enum MouseFlag : uint
    {
        Move = 0x0001,
        LeftDown = 0x0002,
        LeftUp = 0x0004,
        RightDown = 0x0008,
        RightUp = 0x0010,
        MiddleDown = 0x0020,
        MiddleUp = 0x0040,
        XDown = 0x0080,
        XUp = 0x0100,
        VerticalWheel = 0x0800,
        HorizontalWheel = 0x1000,
        VirtualDesk = 0x4000,
        Absolute = 0x8000,
    }
    public enum MouseButton
    {
        LeftButton,
        RightButton,
    }

    public enum InputType : uint
    {
        Mouse = 0,
        Keyboard,
        Hardware
    }

    [Flags]
    public enum DWMThumbnailPropertiesType : uint
    {
        Rectdestination = 0x00000001,
        Rectsource = 0x00000002,
        Opacity = 0x00000004,
        Visible = 0x00000008,
        SourcecLientareaOnly = 0x00000010,
    }

    public enum WindowMessage : uint
    {
        None = 0x000,

        MouseActivate = 0x021,

        KeyDown = 0x100,
        KeyUp,
        Char,

        SysKeyDown = 0x104,
        SysKeyUp,

        SysCommand = 0x112,

        MouseMove = 0x200,
        LButtonDown = 0x201,
        LButtonUp,
        LButtonDoubleClick,
        RButtonDown,
        RButtonUp,
        RButtonDoubleClick,

        MouseWheel = 0x20A,
        ParentNotify = 0x210,

        IMESetContext = 0x281,

        Max
    }

    public enum DpiFlags
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }

    public enum SpecialWindowHandles
    {
        Top = 0,
        Bottom = 1,
        TopMost = -1,
        NoTopMost = -2
    }

    [Flags]
    public enum WindowPosFlags : uint
    {
        AsynchronousWindowPosition = 0x4000,
        DeferErase = 0x2000,
        DrawFrame = 0x0020,
        FrameChanged = 0x0020,
        HideWindow = 0x0080,
        DoNotActivate = 0x0010,
        DoNotCopyBits = 0x0100,
        IgnoreMove = 0x0002,
        DoNotChangeOwnerZOrder = 0x0200,
        DoNotRedraw = 0x0008,
        DoNotReposition = 0x0200,
        DoNotSendChangingEvent = 0x0400,
        IgnoreResize = 0x0001,
        IgnoreZOrder = 0x0004,
        ShowWindow = 0x0040,
    }
}
