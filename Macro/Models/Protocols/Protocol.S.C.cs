using Macro.Infrastructure;

namespace Macro.Models.Protocols
{
    public interface IAPIResponse
    {
        bool Ok { get; }
    }
    public class GetMacroLatestVersionResponse : IAPIResponse
    {
        public bool Ok { get; set; }

        public VersionNote VersionNote { get; set; }
    }

    public class CheckSponsorshipResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public bool IsSponsor { get; set; }
        public string ErrorMessage { get; set; }
    }
}
