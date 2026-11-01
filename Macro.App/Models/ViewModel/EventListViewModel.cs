using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Macro.Models.ViewModel
{
    public class EventListViewModel : INotifyPropertyChanged
    {
        public EventListViewModel(ObservableCollection<EventInfoModel> eventItems)
        {
            _eventItems = eventItems;
        }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isAllSelected = true;
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                _isAllSelected = value;
                OnPropertyChanged("IsAllSelected");
                foreach (var item in EventItems)
                {
                    item.IsChecked = _isAllSelected;
                }
            }
        }
        private ObservableCollection<EventInfoModel> _eventItems;
        public ObservableCollection<EventInfoModel> EventItems
        {
            get => _eventItems;
            set
            {
                _eventItems = value;
                OnPropertyChanged("EventItems");
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
