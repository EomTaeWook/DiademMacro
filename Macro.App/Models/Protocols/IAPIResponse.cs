namespace Macro.Models.Protocols
{
    public interface IAPIResponse
    {
        bool Ok { get; }
        string ErrorMessage { get; }
    }
}
