using DataContainer.Generated;
using Macro.Extensions;
using Macro.Infrastructure;

namespace Macro.Models
{
    public class LabelLocator
    {
        public string Title
        {
            get => $"{GetString(1017)} ver {VersionNote.CurrentVersion.Major}.{VersionNote.CurrentVersion.Minor}.{VersionNote.CurrentVersion.Build}";
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
        public string MouseWheel
        {
            get => GetString(1010);
        }
        public string WheelData
        {
            get => GetString(1011);
        }
        public string TriggerList
        {
            get => GetString(1008);
        }
        public string Config
        {
            get => GetString(1004);
        }
        public string SetROI
        {
            get => GetString(1049);
        }
        public string Save
        {
            get => GetString(1003);
        }
        public string SaveConfig
        {
            get => GetString(1005);
        }
        public string ImageCapture
        {
            get => GetString(1002);
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
            get => GetString(1055);
        }
        public string Delete
        {
            get => GetString(1013);
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
            get => GetString(1052);
        }
        public string MoveProcessLocation
        {
            get => GetString(1053);
        }
        public string RestoreMoveProcessLocation
        {
            get => GetString(1054);
        }
        public string VersionCheck
        {
            get => GetString(1025);
        }
        public string Fix
        {
            get => GetString(1026);
        }
        public string RepeatSubItems
        {
            get => GetString(1027);
        }
        public string Cancel
        {
            get => GetString(1027);
        }
        public string AddSameContent
        {
            get => GetString(1038);
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
        public string X
        {
            get => "X";
        }
        public string Y
        {
            get => "Y";
        }

        public string ImageSearchRequired
        {
            get => GetString(1043);
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
            get => GetString(1057);
        }
        public string RemoveROI
        {
            get => GetString(1050);
        }
        public string MacroMode
        {
            get => GetString(1058);
        }
        private string GetString(int templateId)
        {
            var template = TemplateContainer<LabelTemplate>.Find(templateId);

            return template.GetString();
        }
    }
}
