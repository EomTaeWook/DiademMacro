using Dignus.Coroutine;
using System.Collections;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Controller
{
    internal static class CoroutineRunner
    {
        private static readonly CoroutineHandler _coroutineHandler = new();

        private static readonly Task _executionTask;
        private static volatile bool _stopCalled;
        static CoroutineRunner()
        {
            _executionTask = Task.Run(async () =>
            {
                while (_stopCalled == false)
                {
                    _coroutineHandler.UpdateCoroutines(0.033F);
                    await Task.Delay(33).ConfigureAwait(false);
                }
            });
        }
        public static CoroutineHandle Start(IEnumerator enumerator)
        {
            return Start(0, enumerator);
        }
        public static CoroutineHandle Start(float delay, IEnumerator enumerator)
        {
            return _coroutineHandler.Start(delay, enumerator);
        }
        public static bool Stop(CoroutineHandle coroutineHandle)
        {
            return _coroutineHandler.Stop(coroutineHandle);
        }
        public static async Task StopAsync()
        {
            _stopCalled = true;
            await _executionTask.ConfigureAwait(false);
        }

        public static void Dispose()
        {
            _stopCalled = true;
            _coroutineHandler.StopAll();

        }

    }
}
