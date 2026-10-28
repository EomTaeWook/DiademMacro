using Macro.Infrastructure;

namespace Macro.Models.Protocols
{
    public class GetMacroLatestVersionResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public string ErrorMessage { get; set; }
        public VersionNote VersionNote { get; set; }
    }
}
