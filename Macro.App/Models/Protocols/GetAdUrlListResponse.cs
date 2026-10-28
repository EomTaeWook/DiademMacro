using DataContainer;
using System.Collections.Generic;

namespace Macro.Models.Protocols
{
    public class GetAdUrlListResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public List<AdData> AdUrls { get; set; } = new();
        public string ErrorMessage { get; set; }
    }
}
