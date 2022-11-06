﻿using Macro.Infrastructure;
using Macro.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace Macro.Models.ViewModel
{
    public class EventSettingViewModel : INotifyPropertyChanged
    {
        private double _width;
        private double _height;
        private ObservableCollection<EventTriggerModel> _triggerSaves = new ObservableCollection<EventTriggerModel>();
        private TreeGridViewItem _currentItem = new TreeGridViewItem();
        private PointModel _pointModel = new PointModel();

        private readonly TreeGridViewItem _dummy;
        private readonly PointModel _dummyRelativePosition;

        public EventSettingViewModel()
        {
            _dummy = new TreeGridViewItem()
            {
                DataContext = MakeEventTriggerModel(),
            };
            _dummyRelativePosition = new PointModel();
            _currentItem = _dummy;
            _pointModel = _dummyRelativePosition;
            
        }

        public void Clear()
        {
            CurrentTreeViewItem.IsSelected = false;
            if (CurrentTreeViewItem != _dummy)
            {
                CurrentTreeViewItem = _dummy;
            }
            if(RelativePosition != _dummyRelativePosition)
            {
                RelativePosition = _dummyRelativePosition;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<EventTriggerModel> TriggerSaves
        {
            get => _triggerSaves;
            private set
            {
                _triggerSaves = value;
                OnPropertyChanged("TriggerSaves");
            }
        }
        public bool IsExistence()
        {
            return this.CurrentTreeViewItem != _dummy;
        }
        private EventTriggerModel MakeEventTriggerModel()
        {
            return new EventTriggerModel()
            {
                EventType = Infrastructure.EventType.Image,
                MonitorInfo = new Utils.Infrastructure.MonitorInfo(),
                MouseTriggerInfo = new MouseTriggerInfo(),
                SubEventTriggers = new ObservableCollection<EventTriggerModel>(),
                ProcessInfo = new ProcessInfo(),
                RepeatInfo = new RepeatInfoModel(),
                RoiData = null,
            };
        }
        public TreeGridViewItem CurrentTreeViewItem
        {
            get 
            {
                if(_currentItem == _dummy)
                {
                    _currentItem = new TreeGridViewItem()
                    {
                        DataContext = MakeEventTriggerModel()
                    };
                }
                return _currentItem;
            }
            set
            {
                _currentItem = value;
                OnPropertyChanged("CurrentTreeViewItem");
            }
        }
        public PointModel RelativePosition
        {
            get => _pointModel;
            set
            {
                _pointModel = value;
                OnPropertyChanged("RelativePosition");
            }
        }

        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged("Width");
            }
        }
        public double Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged("Height");
            }
        }
    }
}
