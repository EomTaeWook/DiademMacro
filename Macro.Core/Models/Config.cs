using Macro.Infrastructure;
using System.ComponentModel;
using Utils;
using Utils.Models;

namespace Macro.Models
{
    public class Config : INotifyPropertyChanged
    {
        public Config()
        {
        }

        public Config(Config source)
        {
            CopyFrom(source);
        }

        public void CopyFrom(Config source)
        {
            Language = source.Language;
            SavePath = source.SavePath;
            ProcessPeriod = source.ProcessPeriod;
            ItemDelay = source.ItemDelay;
            Similarity = source.Similarity;
            SearchImageResultDisplay = source.SearchImageResultDisplay;
            VersionCheck = source.VersionCheck;
            DragDelay = source.DragDelay;
            ProcessLocationX = source.ProcessLocationX;
            ProcessLocationY = source.ProcessLocationY;
            AccessKey = source.AccessKey;
            MacroMode = source.MacroMode;
        }

        private LanguageType _language = LanguageType.Kor;
        public LanguageType Language
        {
            get => _language;
            set
            {
                _language = value;
                OnPropertyChanged(nameof(Language));
            }
        }

        private string _savePath = string.Empty;
        public string SavePath
        {
            get => _savePath;
            set
            {
                _savePath = value;
                OnPropertyChanged(nameof(SavePath));
            }
        }
        private int _processPeriod = ConstHelper.MinPeriod;
        public int ProcessPeriod
        {
            get => _processPeriod;
            set
            {
                _processPeriod = value;
                OnPropertyChanged(nameof(ProcessPeriod));
            }
        }
        private int _itemDelay = ConstHelper.MinItemDelay;
        public int ItemDelay
        {
            get => _itemDelay;
            set
            {
                _itemDelay = value;
                OnPropertyChanged(nameof(ItemDelay));
            }
        }
        private int _similarity = ConstHelper.DefaultSimilarity;
        public int Similarity
        {
            get => _similarity;
            set
            {
                _similarity = value;
                OnPropertyChanged(nameof(Similarity));
            }
        }
        private bool _searchImageResultDisplay = true;
        public bool SearchImageResultDisplay
        {
            get => _searchImageResultDisplay;
            set
            {
                _searchImageResultDisplay = value;
                OnPropertyChanged(nameof(SearchImageResultDisplay));
            }
        }
        private bool _versionCheck = true;
        public bool VersionCheck
        {
            get => _versionCheck;
            set
            {
                _versionCheck = value;
                OnPropertyChanged(nameof(VersionCheck));
            }
        }
        private int _dragDelay = ConstHelper.MinDragDelay;
        public int DragDelay
        {
            get => _dragDelay;
            set
            {
                _dragDelay = value;
                OnPropertyChanged(nameof(DragDelay));
            }
        }
        private int _processLocationX;
        public int ProcessLocationX
        {
            get => _processLocationX;
            set
            {
                _processLocationX = value;
                OnPropertyChanged(nameof(ProcessLocationX));
            }
        }
        private int _processLocationY;
        public int ProcessLocationY
        {
            get => _processLocationY;
            set
            {
                _processLocationY = value;
                OnPropertyChanged(nameof(ProcessLocationY));
            }
        }

        private string _accessKey;
        public string AccessKey
        {
            get => _accessKey;

            set
            {
                _accessKey = value;
                OnPropertyChanged(nameof(AccessKey));
            }
        }
        private MacroModeType _macroMode = MacroModeType.BatchMode;
        public MacroModeType MacroMode
        {
            get => _macroMode;

            set
            {
                _macroMode = value;
                OnPropertyChanged(nameof(MacroMode));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
