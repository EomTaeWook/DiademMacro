using Macro.Infrastructure;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Macro.Models.ViewModel
{
    public class OptionDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<EventType> EventTypeList { get; set; } = new ObservableCollection<EventType>()
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
        private int _eventToNext;
        public int EventToNext
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
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
