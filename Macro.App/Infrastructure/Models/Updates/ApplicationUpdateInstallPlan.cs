using System;
using System.Diagnostics;

namespace Macro.Infrastructure.Models.Updates
{
    public class ApplicationUpdateInstallPlan
    {
        internal ApplicationUpdateInstallPlan(string directory, string nonce, VerifiedApplicationUpdate download)
        {
            Directory = directory;
            Nonce = nonce;
            Download = download;
        }

        internal VerifiedApplicationUpdate Download { get; }
        internal string Directory { get; }
        internal string Nonce { get; }
        internal Process Helper { get; set; }
    }
}
