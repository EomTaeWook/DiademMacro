using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Macro.Models.ViewModel
{
    public class EventListViewModel : INotifyPropertyChanged
    {
        private double _width;
        private double _height;
        private bool _isAllSelected;

        public event PropertyChangedEventHandler PropertyChanged;

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
        public bool IsAllSelected
        {
            get => _isAllSelected;
            set
            {
                _isAllSelected = value;
                OnPropertyChanged("IsAllSelected");
            }
        }
        private ObservableCollection<EventInfoModel> _eventItems = new ObservableCollection<EventInfoModel>();
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
