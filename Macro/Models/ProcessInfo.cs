using System;
using Utils.Infrastructure;

namespace Macro.Models
{
    [Serializable]
    public class ProcessInfo
    {
        public string ProcessName { get; set; }
        public IntRect Position { get; set; } = new IntRect();
    }
}
