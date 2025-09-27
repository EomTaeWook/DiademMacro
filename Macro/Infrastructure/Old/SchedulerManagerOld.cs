using Dignus.Framework;
using Dignus.Log;
using Macro.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using Utils.Infrastructure;

namespace Macro.Infrastructure.Manager
{
    public class SchedulerManagerOld : Singleton<SchedulerManagerOld>
    {
        private CancellationTokenSource cts;
        private CancellationToken cancellationToken = CancellationToken.None;
        public SchedulerManagerOld()
        {
        }
        public bool IsRunning()
        {
            return cancellationToken != CancellationToken.None;
        }
        public Task Start()
        {
            if (IsRunning() == true)
            {
                LogHelper.Error($"scheduler is already running!");
                return Task.CompletedTask;
            }
            cts = new CancellationTokenSource();
            cancellationToken = cts.Token;
            return TaskBuilder.Build(async () => { await UpdateAsync(); });
        }
        public void Stop()
        {
            cts.Cancel();
            cts.Dispose();
            cancellationToken = CancellationToken.None;
        }
        public async Task UpdateAsync()
        {
            long previousTick = DateTime.Now.Ticks;
            var args = new UpdatedTimeArgs();
            while (IsRunning() == true)
            {
                await Task.Delay(33);
                var currentTime = DateTime.Now.Ticks;
                args.DeltaTime = (float)TimeSpan.FromTicks(currentTime - previousTick).TotalSeconds;
                previousTick = currentTime;

                //NotifyHelperOld.InvokeNotify(NotifyEventOldType.UpdatedTime, args);
            }
        }
    }
}
