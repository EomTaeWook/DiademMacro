using Macro.Infrastructure;
using Macro.Infrastructure.Serialize;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using Utils.Infrastructure;

namespace Macro.Models
{
    [Serializable]
    public class EventTriggerModel : INotifyPropertyChanged
    {
        public static EventTriggerModel DummyParentEventModel;

        private EventType _eventType = EventType.Image;
        private MouseEventInfo _mouseEventInfo;
        private string _keyboardCmd = "";
        private ProcessInfo _processInfo;
        private ObservableCollection<EventTriggerModel> _subEventItems;
        private int _afterDelay;
        private RepeatInfoModel _repeatInfo;
        private ulong _eventToNext = 0;
        private ulong _triggerIndex = 0;
        private bool _imageSearchRequired = false;
        private bool _sameImageDrag = false;
        private bool _hardClick = false;
        private int _maxDragCount = 1;
        private bool _isChecked = true;
        private RoiModel _roiData = new RoiModel();
        private Bitmap _image;

        public EventTriggerModel()
        {
        }
        public EventTriggerModel(EventTriggerModel other)
        {
            _image = other.Image;
            _eventType = other.EventType;
            _mouseEventInfo = other.MouseEventInfo;
            _keyboardCmd = other.KeyboardCmd;
            _processInfo = other.ProcessInfo;
            _subEventItems = other.SubEventItems;
            _afterDelay = other.AfterDelay;
            _repeatInfo = other.RepeatInfo;
            _eventToNext = other.EventToNext;
            _triggerIndex = other.TriggerIndex;
            _sameImageDrag = other.SameImageDrag;
            _maxDragCount = other.MaxDragCount;
            _hardClick = other._hardClick;
            _roiData = other._roiData;
            _isChecked = other._isChecked;
        }

        [Order(1)]
        public Bitmap Image
        {
            get => _image;
            set => _image = value;
        }

        [Order(2)]
        public EventType EventType
        {
            get => _eventType;
            set
            {
                _eventType = value;
                OnPropertyChanged(nameof(EventType));
            }
        }

        [Order(3)]
        public MouseEventInfo MouseEventInfo
        {
            get => _mouseEventInfo ?? (_mouseEventInfo = new MouseEventInfo());
            set
            {
                _mouseEventInfo = value;
                OnPropertyChanged(nameof(MouseEventInfo));
            }
        }

        [Order(4)]
        public MonitorInfo MonitorInfo { get; set; }

        [Order(5)]
        public string KeyboardCmd
        {
            get => _keyboardCmd;
            set
            {
                _keyboardCmd = value;
                OnPropertyChanged(nameof(KeyboardCmd));
            }
        }

        [Order(6)]
        public ProcessInfo ProcessInfo
        {
            get => _processInfo ?? (_processInfo = new ProcessInfo() { Position = new IntRect(), });
            set
            {
                _processInfo = value;
                OnPropertyChanged(nameof(ProcessInfo));
            }
        }
        [Order(7)]
        public ObservableCollection<EventTriggerModel> SubEventItems
        {
            get => _subEventItems ?? (_subEventItems = new ObservableCollection<EventTriggerModel>());
            set
            {
                _subEventItems = value;
                OnPropertyChanged(nameof(SubEventItems));
            }
        }

        [Order(8)]
        public int AfterDelay
        {
            get => _afterDelay;
            set
            {
                _afterDelay = value;
                OnPropertyChanged(nameof(AfterDelay));
            }
        }
        [Order(9)]
        public RepeatInfoModel RepeatInfo
        {
            get => _repeatInfo ?? (_repeatInfo = new RepeatInfoModel());
            set
            {
                _repeatInfo = value;
                OnPropertyChanged(nameof(RepeatInfo));
            }
        }
        [Order(10)]
        public ulong TriggerIndex
        {
            set
            {
                _triggerIndex = value;
                OnPropertyChanged(nameof(TriggerIndex));
            }
            get => _triggerIndex;
        }

        [Order(11)]
        public ulong EventToNext
        {
            set
            {
                _eventToNext = value;
                OnPropertyChanged(nameof(EventToNext));
            }
            get => _eventToNext;
        }

        [Order(13)]
        public bool SameImageDrag
        {
            set
            {
                _sameImageDrag = value;
                OnPropertyChanged(nameof(SameImageDrag));
            }
            get => _sameImageDrag;
        }
        [Order(14)]
        public int MaxDragCount
        {
            set
            {
                _maxDragCount = value;
                OnPropertyChanged(nameof(MaxDragCount));
            }
            get => _maxDragCount;
        }
        [Order(15)]
        public bool HardClick
        {
            set
            {
                _hardClick = value;
                OnPropertyChanged(nameof(HardClick));
            }
            get => _hardClick;
        }
        [Order(16)]
        public RoiModel RoiDataInfo
        {
            set
            {
                _roiData = value;
                OnPropertyChanged(nameof(RoiDataInfo));
            }
            get => _roiData;
        }
        [Order(17)]
        public bool IsChecked
        {
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
            get => _isChecked;
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
