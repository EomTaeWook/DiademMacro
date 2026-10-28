using System;
using System.ComponentModel;

namespace Macro.Models
{
    [Serializable]
    public class PointModel : INotifyPropertyChanged
    {
        private double _x;
        private double _y;
        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged("X");
            }
        }
        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged("Y");
            }
        }

        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
