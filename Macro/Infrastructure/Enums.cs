namespace Macro.Infrastructure
{
    public enum EventType
    {
        Mouse,
        Keyboard,
        Image,
        RelativeToImage,

        Max
    }
    public enum MouseEventType
    {
        None,
        LeftClick,
        RightClick,
        Drag,
        Wheel,

        Max
    }

    public enum NotifyEventType
    {
        ComboProcessChanged,

        ConfigChanged,
        MousePointDataBind,
        ScreenCaptureDataBind,
        TreeItemOrderChanged,
        SelctTreeViewItemChanged,
        EventTriggerOrderChanged,
        EventTriggerInserted,
        EventTriggerRemoved,

        Save,
        Delete,

        TreeGridViewFocus,

        UpdatedTime,
        ROICaptureDataBind,

        Max
    }

    public enum RepeatType
    {
        Count,
        NoSearchChild,
        SearchParent,

        Max
    }

    public enum ConditionType
    {
        Above,
        Below,

        Max
    }

    public enum CaptureModeType
    {
        ImageCapture,
        ROICapture,

        Max
    }
    public enum MacroModeType
    {
        SequentialMode, // 순차 모드
        BatchMode       // 배치 모드
    }

    public enum InitialTab
    {
        Common,
        //Game,

        Max
    }
}
