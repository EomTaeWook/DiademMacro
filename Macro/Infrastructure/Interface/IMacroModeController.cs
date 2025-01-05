using Dignus.Collections;
using Macro.Models;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace Macro.Infrastructure.Interfaces
{
    internal interface IMacroModeController
    {
        void Execute(Action<Bitmap> drawImageCallback,
            ArrayQueue<Process> processes,
            ArrayQueue<EventTriggerModel> eventTriggerModels,
            CancellationToken cancellationToken);
    }
}
