using System;
namespace Macro.Infrastructure.Models.Updates
{
    public class ApplicationUpdateProgress
    {
        internal ApplicationUpdateProgress(long bytesReceived, long totalBytes)
        {
            BytesReceived = bytesReceived;
            TotalBytes = totalBytes;
        }

        public long BytesReceived { get; }
        public long TotalBytes { get; }
        public double Percentage
        {
            get
            {
                return BytesReceived * 100.0 / TotalBytes;
            }
        }
    }
}
