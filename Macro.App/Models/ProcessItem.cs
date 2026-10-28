using System.Diagnostics;

namespace Macro.Models
{
    public class ProcessItem : System.IDisposable
    {
        public string ProcessName { get => _processName; }

        public Process Process { get; private set; }

        private readonly string _processName;
        public ProcessItem(Process process)
        {
            Process = process;
            _processName = $"{Process.ProcessName}:{Process.Id}";
        }

        public void Dispose()
        {
            Process.Dispose();
        }
    }
}
