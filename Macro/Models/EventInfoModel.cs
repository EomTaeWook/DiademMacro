using Macro.Extensions;
using Macro.Infrastructure;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using Utils.Infrastructure;
using Utils.Serialization;

namespace Macro.Models
{
    public class EventInfoModel : INotifyPropertyChanged
    {
        [JsonConstructor]
        public EventInfoModel()
        {
        }
        public EventInfoModel(EventInfoModel other)
        {
            Image = new Bitmap(other.Image);
            EventType = other.EventType;
            MouseEventInfo = other.MouseEventInfo.Clone();
            MonitorInfo = other.MonitorInfo.Clone();

            KeyboardCmd = other.KeyboardCmd;
            ProcessInfo = other.ProcessInfo.Clone();
            SubEventItems = new ObservableCollection<EventInfoModel>(other.SubEventItems);
            AfterDelay = other.AfterDelay;
            RepeatInfo = other.RepeatInfo.Clone();
            EventToNext = other.EventToNext;
            SameImageDrag = other.SameImageDrag;
            HardClick = other.HardClick;
            RoiDataInfo = other.RoiDataInfo.Clone();
            IsChecked = other.IsChecked;
        }
        private Bitmap _image;
        [JsonConverter(typeof(BitmapFileJsonConverter))]
        public Bitmap Image
        {
            get => _image;
            set => _image = value;
        }
        private EventType _eventType = EventType.Image;
        public EventType EventType
        {
            get => _eventType;
            set
            {
                _eventType = value;
                OnPropertyChanged(nameof(EventType));
            }
        }
        private MouseEventInfoV2 _mouseEventInfo;
        public MouseEventInfoV2 MouseEventInfo
        {
            get => _mouseEventInfo ?? (_mouseEventInfo = new MouseEventInfoV2());
            set
            {
                _mouseEventInfo = value;
                OnPropertyChanged(nameof(MouseEventInfo));
            }
        }

        public MonitorInfo MonitorInfo { get; set; }

        private string _keyboardCmd = string.Empty;
        public string KeyboardCmd
        {
            get => _keyboardCmd;
            set
            {
                _keyboardCmd = value;
                OnPropertyChanged(nameof(KeyboardCmd));
            }
        }
        private ProcessInfo _processInfo;
        public ProcessInfo ProcessInfo
        {
            get
            {
                if (_processInfo == null)
                {
                    _processInfo = new ProcessInfo();
                }
                return _processInfo;
            }
            set
            {
                _processInfo = value;
                OnPropertyChanged(nameof(ProcessInfo));
            }
        }

        private ObservableCollection<EventInfoModel> _subEventItems;
        public ObservableCollection<EventInfoModel> SubEventItems
        {
            get
            {
                if (_subEventItems == null)
                {
                    _subEventItems = new ObservableCollection<EventInfoModel>();
                }
                return _subEventItems;
            }
            set
            {
                _subEventItems = value;
                OnPropertyChanged(nameof(SubEventItems));
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

        private RepeatInfoModel _repeatInfo;
        public RepeatInfoModel RepeatInfo
        {
            get
            {
                if (_repeatInfo == null)
                {
                    _repeatInfo = new RepeatInfoModel();
                }
                return _repeatInfo;
            }
            set
            {
                _repeatInfo = value;
                OnPropertyChanged(nameof(RepeatInfo));
            }
        }

        private ulong _itemIndex = 0;
        public ulong ItemIndex
        {
            set
            {
                _itemIndex = value;
                OnPropertyChanged(nameof(ItemIndex));
            }
            get => _itemIndex;
        }

        private ulong _eventToNext = 0;
        public ulong EventToNext
        {
            set
            {
                _eventToNext = value;
                OnPropertyChanged(nameof(EventToNext));
            }
            get => _eventToNext;
        }

        private bool _sameImageDrag = false;
        public bool SameImageDrag
        {
            set
            {
                _sameImageDrag = value;
                OnPropertyChanged(nameof(SameImageDrag));
            }
            get => _sameImageDrag;
        }
        private int _maxDragCount = 1;
        public int MaxDragCount
        {
            set
            {
                _maxDragCount = value;
                OnPropertyChanged(nameof(MaxDragCount));
            }
            get => _maxDragCount;
        }

        private bool _hardClick = false;
        public bool HardClick
        {
            set
            {
                _hardClick = value;
                OnPropertyChanged(nameof(HardClick));
            }
            get => _hardClick;
        }
        private RoiModel _roiData = new RoiModel();
        public RoiModel RoiDataInfo
        {
            set
            {
                _roiData = value;
                OnPropertyChanged(nameof(RoiDataInfo));
            }
            get => _roiData;
        }

        private bool _isChecked = true;
        public bool IsChecked
        {
            set
            {
                _isChecked = value;
                OnPropertyChanged(nameof(IsChecked));
            }
            get => _isChecked;
        }

        private Point2D? _positionRelativeToImage;
        public Point2D PositionRelativeToImage
        {
            get
            {
                if (_positionRelativeToImage == null)
                {
                    _positionRelativeToImage = new Point2D();
                }
                return _positionRelativeToImage.Value;
            }
            set
            {
                _positionRelativeToImage = value;
                OnPropertyChanged(nameof(PositionRelativeToImage));
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
