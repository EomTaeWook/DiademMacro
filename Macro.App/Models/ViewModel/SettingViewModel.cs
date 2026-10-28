using Macro.Command;
using System.ComponentModel;
using Dignus.DependencyInjection.Attributes;
using System.Windows.Input;

namespace Macro.Models.ViewModel
{
    [Injectable(Dignus.DependencyInjection.LifeScope.Transient)]
    public class SettingViewModel : INotifyPropertyChanged
    {
        private readonly Config _sourceConfig;
        private Config _config;
        private ICommand _savePathButtonCommand;

        public SettingViewModel(Config config)
        {
            _sourceConfig = config;
            _config = new Config(config);
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

        public ICommand SavePathButtonCommand
        {
            get => _savePathButtonCommand ?? (_savePathButtonCommand = new FolderBrowserDialogCommand());
        }

        public Config ApplyChanges()
        {
            _sourceConfig.CopyFrom(_config);
            return _sourceConfig;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
