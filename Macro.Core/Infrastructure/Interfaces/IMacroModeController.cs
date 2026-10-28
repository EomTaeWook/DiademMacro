using Dignus.Collections;
using Macro.Models;
using System.Diagnostics;
using System.Threading;

namespace Macro.Infrastructure.Interfaces
{
    public interface IMacroModeController
    {
        void Execute(ArrayQueue<Process> processes, ArrayQueue<EventInfoModel> eventTriggerModels, CancellationToken cancellationToken);
    }
}
