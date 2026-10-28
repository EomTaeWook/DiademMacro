namespace Macro.Models.Protocols
{
    public class OnePickAdUrlResponse : IAPIResponse
    {
        public bool Ok { get; set; }
        public string ErrorMessage { get; set; }
        public string AdUrl { get; set; }
    }
}
