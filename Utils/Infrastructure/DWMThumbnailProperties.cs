using System.Runtime.InteropServices;

namespace Utils.Infrastructure
{
    [StructLayout(LayoutKind.Sequential)]
    public struct DWMThumbnailProperties
    {
        public uint Flags;
        public IntRect Destination;
        public IntRect Source;
        public byte Opacity;
        public bool Visible;
        public bool SourceClientAreaOnly;
    }
}
