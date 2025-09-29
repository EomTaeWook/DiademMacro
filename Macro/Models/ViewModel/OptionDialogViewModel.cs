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

        private string _selectedEventType;
        public string SelectedEventType
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

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
