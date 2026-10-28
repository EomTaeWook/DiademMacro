namespace Macro.Models.Protocols
{
    public class CheckSponsorshipResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsSponsor { get; set; }
    }
}
