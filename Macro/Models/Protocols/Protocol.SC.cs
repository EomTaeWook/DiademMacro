using DataContainer;
using Macro.Infrastructure;
using System.Collections.Generic;

namespace Macro.Models.Protocols
{
    public interface IAPIResponse
    {
        bool Ok { get; }

        string ErrorMessage { get; }
    }
    public class GetMacroLatestVersionResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public string ErrorMessage { get; set; }
        public VersionNote VersionNote { get; set; }
    }

    public class CheckSponsorshipResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSponsor { get; set; }
    }

    public class GetAdUrlListResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public List<AdData> AdUrls { get; set; } = new List<AdData>();
        public string ErrorMessage { get; set; }
    }
}
