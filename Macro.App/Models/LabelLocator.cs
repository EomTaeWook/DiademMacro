using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;
using System.ComponentModel;

namespace Macro.Models
{
    public class LabelLocator : INotifyPropertyChanged
    {
        public LabelLocator()
        {
            NotifyHelper.ConfigChanged += NotifyHelper_ConfigChanged;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title
        {
            get => $"{GetString(1017)} {VersionNote.CurrentVersion.Major}.{VersionNote.CurrentVersion.Minor}.{VersionNote.CurrentVersion.Build}";
        }
        public string EventType
        {
            get => GetString(1006);
        }
        public string EventDataSet
        {
            get => GetString(1007);
        }
        public string MouseCoordinates
        {
            get => GetString(1009);
        }
        public string TriggerList
        {
            get => GetString(1008);
        }
        public string SetROI
        {
            get => GetString(1049);
        }
        public string Save
        {
            get => GetString(1003);
        }
        public string CompareImage
        {
            get => GetString(1001);
        }
        public string SelectProcess
        {
            get => GetString(1000);
        }
        public string Refresh
        {
            get => GetString(1054);
        }
        public string Start
        {
            get => GetString(1014);
        }
        public string Stop
        {
            get => GetString(1015);
        }
        public string Setting
        {
            get => GetString(1016);
        }
        public string OpenLogFolder
        {
            get => GetString(1087);
        }
        public string McpServer
        {
            get
            {
                return GetString(1088);
            }
        }
        public string MacPermissions
        {
            get
            {
                return GetString(1098);
            }
        }
        public string Advertisement
        {
            get
            {
                return GetString(1104);
            }
        }
        public string AdvertisementTitle
        {
            get
            {
                return GetString(1108);
            }
        }
        public string AdvertisementDetails
        {
            get
            {
                return GetString(1105);
            }
        }
        public string AdvertisementReady
        {
            get
            {
                return GetString(1107);
            }
        }
        public string RequestInputPermission
        {
            get
            {
                return GetString(1099);
            }
        }
        public string RequestCapturePermission
        {
            get
            {
                return GetString(1100);
            }
        }
        public string PermissionGranted
        {
            get
            {
                return GetString(1101);
            }
        }
        public string PermissionRequired
        {
            get
            {
                return GetString(1102);
            }
        }
        public string MacPermissionDescription
        {
            get
            {
                return GetString(1103);
            }
        }
        public string McpOpen
        {
            get
            {
                return GetString(1089);
            }
        }
        public string McpClose
        {
            get
            {
                return GetString(1090);
            }
        }
        public string McpConnectionAddress
        {
            get
            {
                return GetString(1091);
            }
        }
        public string McpPort
        {
            get
            {
                return GetString(1097);
            }
        }
        public string McpOpenStatus
        {
            get
            {
                return GetString(1092);
            }
        }
        public string McpClosedStatus
        {
            get
            {
                return GetString(1093);
            }
        }
        public string McpDescription
        {
            get
            {
                return GetString(1094);
            }
        }
        public string Language
        {
            get => GetString(1018);
        }
        public string SavePath
        {
            get => GetString(1019);
        }
        public string Period
        {
            get => GetString(1020);
        }
        public string ItemDelay
        {
            get => GetString(1021);
        }
        public string Similarity
        {
            get => GetString(1022);
        }
        public string ItemSimilarity
        {
            get => GetString(1066);
        }
        public string AfterDelay
        {
            get => GetString(1023);
        }
        public string SearchResultDisplay
        {
            get => GetString(1024);
        }
        public string SettingProcessLocation
        {
            get => GetString(1051);
        }
        public string Roi
        {
            get => GetString(1064);
        }
        public string Fix
        {
            get => GetString(1026);
        }
        public string RepeatItems
        {
            get => GetString(1027);
        }
        public string TriggerToNext
        {
            get => GetString(1039);
        }
        public string DragDelay
        {
            get => GetString(1047);
        }
        public string Preferences
        {
            get => GetString(1005);
        }
        public string Close
        {
            get => GetString(1012);
        }
        public string SameImageDrag
        {
            get => GetString(1045);
        }
        public string HardClick
        {
            get => GetString(1048);
        }
        public string FindTheSameImageRepeatedly
        {
            get => GetString(1046);
        }
        public string Common
        {
            get => GetString(1056);
        }
        public string RemoveROI
        {
            get => GetString(1050);
        }
        public string MacroMode
        {
            get => GetString(1057);
        }
        public string Addition
        {
            get => GetString(1059);
        }
        public string Removal
        {
            get => GetString(1060);
        }
        public string Copy
        {
            get => GetString(1061);
        }
        public string Option
        {
            get => GetString(1063);
        }

        public string Image
        {
            get
            {
                return GetString(1073);
            }
        }
        public string ItemIndex
        {
            get
            {
                return GetString(1074);
            }
        }
        public string ProcessName
        {
            get
            {
                return GetString(1075);
            }
        }
        public string Status
        {
            get
            {
                return GetString(1076);
            }
        }
        public string MoveUp
        {
            get
            {
                return GetString(1077);
            }
        }
        public string MoveDown
        {
            get
            {
                return GetString(1078);
            }
        }
        public string InputSettings
        {
            get
            {
                return GetString(1079);
            }
        }
        public string ExecutionSettings
        {
            get
            {
                return GetString(1080);
            }
        }
        public string DisplaySettings
        {
            get
            {
                return GetString(1081);
            }
        }
        public string EmptyEventList
        {
            get
            {
                return GetString(1082);
            }
        }
        public string EmptyEventHint
        {
            get
            {
                return GetString(1083);
            }
        }
        public string PhysicalClickDescription
        {
            get
            {
                return GetString(1084);
            }
        }
        public string SelectAll
        {
            get
            {
                return GetString(1085);
            }
        }
        public string ExecutionHint
        {
            get
            {
                return GetString(1086);
            }
        }

        private string GetString(int templateId)
        {
            var template = TemplateContainer<LabelTemplate>.Find(templateId);

            return template.GetString();
        }

        private void NotifyHelper_ConfigChanged(ConfigEventArgs eventArgs)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
