namespace Macro.Models.Protocols
{
    public interface IAPIRequest
    {
    }
    public class GetMacroLatestVersion : IAPIRequest
    {
    }

    public class CheckSponsorship : IAPIRequest
    {
        public string AccessKey { get; set; }
    }
}
