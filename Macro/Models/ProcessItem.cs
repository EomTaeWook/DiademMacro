using System.Diagnostics;

namespace Macro.Models
{
    public class ProcessItem
    {
        public string ProcessName { get => _processName; }

        public Process Process { get; private set; }

        private string _processName;
        public ProcessItem(Process process)
        {
            Process = process;
            _processName = $"{Process.ProcessName}:{Process.Id}";
        }
    }
}
