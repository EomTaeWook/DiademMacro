using Macro.Infrastructure.Models.Updates;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System;
namespace Macro.Infrastructure.Updates
{
    public class ApplicationUpdateException : Exception
    {
        public ApplicationUpdateMessageCode Code { get; }
        public object[] Arguments { get; }

        public ApplicationUpdateException(ApplicationUpdateMessageCode code, params object[] arguments)
            : this(code, null, arguments)
        {
        }

        public ApplicationUpdateException(ApplicationUpdateMessageCode code, Exception innerException, params object[] arguments)
            : base(code.ToString(), innerException)
        {
            Code = code;
            Arguments = arguments;
        }

        public Uri ReleasePage { get; internal set; }
    }
}
