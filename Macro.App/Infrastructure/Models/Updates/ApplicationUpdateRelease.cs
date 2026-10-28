using System;
namespace Macro.Infrastructure.Models.Updates
{
    public class ApplicationUpdateRelease
    {
        internal ApplicationUpdateRelease(string version, string tag, Uri page, Uri asset, string digest, long size)
        {
            Version = version;
            Tag = tag;
            ReleasePage = page;
            AssetUrl = asset;
            Sha256 = digest;
            Size = size;
        }

        public string Version { get; }
        public string Tag { get; }
        public Uri ReleasePage { get; }
        internal Uri AssetUrl { get; }
        public string Sha256 { get; }
        public long Size { get; }
        public bool HasDigest
        {
            get
            {
                return Sha256 != null;
            }
        }
    }
}
