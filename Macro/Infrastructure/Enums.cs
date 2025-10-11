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
        ConfigChanged,
        ScreenCaptureCompleted,
        ROICaptureCompleted,
        EventTriggerSaved,
        MouseInteractionCaptured,

        Max
    }

    public enum NotifyEventOldType
    {
        ComboProcessChanged,

        ConfigChanged,
        MousePointDataBind,
        ScreenCaptureCompleted,
        TreeItemOrderChanged,
        SelctTreeViewItemChanged,
        EventTriggerOrderChanged,
        EventTriggerInserted,
        EventTriggerRemoved,

        Save,
        Delete,

        TreeGridViewFocus,
        ROICaptureCompleted,

        Max
    }

    public enum RepeatType
    {
        Count,
        RepeatOnChildEvent,
        StopOnParentImage,

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
        SequentialMode,
        BatchMode
    }

    public enum InitialTab
    {
        Common,
        //Game,

        Max
    }
}
