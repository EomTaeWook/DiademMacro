using Macro.Infrastructure;
using System;
using System.ComponentModel;

namespace Macro.Models
{
    [Serializable]
    public class RepeatInfoModel : INotifyPropertyChanged
    {
        private RepeatType _repeatType;
        private ushort _count;
        public RepeatType RepeatType
        {
            get => _repeatType;
            set
            {
                _repeatType = value;
                if (_repeatType != RepeatType.Count)
                {
                    Count = 1;
                    OnPropertyChanged("Count");
                }
                OnPropertyChanged("RepeatType");
            }
        }
        public ushort Count
        {
            get => _count;
            set
            {
                _count = value;
                OnPropertyChanged("Count");
            }
        }
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        public RepeatInfoModel()
        {
            _repeatType = RepeatType.Count;
            _count = 1;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
