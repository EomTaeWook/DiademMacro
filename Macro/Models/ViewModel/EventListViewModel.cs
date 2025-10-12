using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Macro.Models.ViewModel
{
    public class EventListViewModel : INotifyPropertyChanged
    {


        public event PropertyChangedEventHandler PropertyChanged;
        private double _width;
        public double Width
        {
            get => _width;
            set
            {
                _width = value;
                OnPropertyChanged("Width");
            }
        }
        private double _height;
        public double Height
        {
            get => _height;
            set
            {
                _height = value;
                OnPropertyChanged("Height");
            }
        }
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
