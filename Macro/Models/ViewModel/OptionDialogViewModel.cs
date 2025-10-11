using Macro.Infrastructure;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Macro.Models.ViewModel
{
    public class OptionDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public ObservableCollection<KeyValuePair<RepeatType, string>> RepeatItemsSource { get; set; } = new ObservableCollection<KeyValuePair<RepeatType, string>>();

        public ObservableCollection<EventType> EventTypes { get; set; } = new ObservableCollection<EventType>()
        {
            EventType.Image,
            EventType.Mouse,
            EventType.Keyboard,
            EventType.RelativeToImage
        };

        private EventType _selectedEventType;
        public EventType SelectedEventType
        {
            get => _selectedEventType;
            set
            {
                _selectedEventType = value;
                OnPropertyChanged(nameof(SelectedEventType));
            }
        }

        private string _keyboardCmd;
        public string KeyboardCmd
        {
            get => _keyboardCmd;
            set
            {
                _keyboardCmd = value;
                OnPropertyChanged(nameof(KeyboardCmd));
            }
        }
        private bool _sameImageDrag;
        public bool SameImageDrag
        {
            get => _sameImageDrag;
            set
            {
                _sameImageDrag = value;
                OnPropertyChanged(nameof(SameImageDrag));
            }
        }
        private int _afterDelay;
        public int AfterDelay
        {
            get => _afterDelay;
            set
            {
                _afterDelay = value;
                OnPropertyChanged(nameof(AfterDelay));
            }
        }
        private ulong _eventToNext;
        public ulong EventToNext
        {
            get => _eventToNext;
            set
            {
                _eventToNext = value;
                OnPropertyChanged(nameof(EventToNext));
            }
        }
        private bool _hardClick;
        public bool HardClick
        {
            get => _hardClick;
            set
            {
                _hardClick = value;
                OnPropertyChanged(nameof(HardClick));
            }
        }

        private RepeatType _selectedRepeatType;
        public RepeatType SelectedRepeatType
        {
            get => _selectedRepeatType;
            set
            {
                _selectedRepeatType = value;
                OnPropertyChanged(nameof(SelectedRepeatType));
            }
        }
        private ushort _repeatCount;
        public ushort RepeatCount
        {
            get => _repeatCount;
            set
            {
                _repeatCount = value;
                OnPropertyChanged(nameof(RepeatCount));
            }
        }
        private MouseEventInfoV2 _mouseEventInfo;

        public MouseEventInfoV2 MouseEventInfo
        {
            get => _mouseEventInfo;
            set
            {
                _mouseEventInfo = value;
                OnPropertyChanged(nameof(MouseEventInfo));

                if (_mouseEventInfo == null)
                {
                    MousePointDesc = "None";
                }
                else
                {
                    MousePointDesc = $"E: {_mouseEventInfo.MouseInfoEventType} X: {_mouseEventInfo.MousePoint.X} Y:{_mouseEventInfo.MousePoint.Y}";
                }
            }
        }
        public string _mousePointDesc;
        public string MousePointDesc
        {
            get => _mousePointDesc;
            set
            {
                _mousePointDesc = value;
                OnPropertyChanged(nameof(MousePointDesc));
            }
        }
        private int _maxDragCount;
        public int MaxDragCount
        {
            get => _maxDragCount;
            set
            {
                _maxDragCount = value;
                OnPropertyChanged(nameof(MaxDragCount));
            }
        }

        private int _positionRelativeToImageX;
        public int PositionRelativeToImageX
        {
            get => _positionRelativeToImageX;
            set
            {
                _positionRelativeToImageX = value;
                OnPropertyChanged(nameof(PositionRelativeToImageX));
            }
        }
        private int _positionRelativeToImageY;
        public int PositionRelativeToImageY
        {
            get => _positionRelativeToImageY;
            set
            {
                _positionRelativeToImageY = value;
                OnPropertyChanged(nameof(PositionRelativeToImageY));
            }
        }

        private RoiModel _roiModel;
        public RoiModel RoiDataInfo
        {
            get => _roiModel;
            set
            {
                _roiModel = value;
                OnPropertyChanged(nameof(RoiDataInfo));

                if (_roiModel.IsExists() == false)
                {
                    RoiDesc = "None";
                }
                else
                {
                    RoiDesc = $"X: {_roiModel.RoiRect.Left} W: {_roiModel.RoiRect.Width} Y:{_roiModel.RoiRect.Top} H:{_roiModel.RoiRect.Height}";
                }
            }
        }
        private string _roiDesc = "None";
        public string RoiDesc
        {
            get => _roiDesc;
            set
            {
                _roiDesc = value;
                OnPropertyChanged(nameof(RoiDesc));
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
