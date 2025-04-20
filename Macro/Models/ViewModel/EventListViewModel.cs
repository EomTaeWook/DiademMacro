using System.Collections.Generic;
using System.ComponentModel;

namespace Macro.Models.ViewModel
{
    public class EventListViewModel : INotifyPropertyChanged
    {
        private double _width;
        private double _height;
        private bool _isAllSelected;
        private List<EventTriggerModel> _triggerSaves;

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
        public List<EventTriggerModel> TriggerSaves
        {
            get => _triggerSaves;
            set
            {
                _triggerSaves = value;
                OnPropertyChanged("TriggerSaves");
            }
        }
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
