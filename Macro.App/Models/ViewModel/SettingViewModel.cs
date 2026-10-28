using Macro.Command;
using System.ComponentModel;
using Dignus.DependencyInjection.Attributes;
using System.Windows.Input;

namespace Macro.Models.ViewModel
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class SettingViewModel : INotifyPropertyChanged
    {
        private Config _config;
        private ICommand _savePathButtonCmd;

        public SettingViewModel(Config config)
        {
            _config = config;
        }
        public Config Config
        {
            get => _config;
            set
            {
                _config = value;
                OnPropertyChanged("Config");
            }
        }

        public ICommand SavePathButtonCmd
        {
            get => _savePathButtonCmd ?? (_savePathButtonCmd = new FolderBrowserDialogCommand());
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
