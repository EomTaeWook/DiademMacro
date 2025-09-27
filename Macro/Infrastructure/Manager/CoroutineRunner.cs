using Dignus.Coroutine;
using System.Collections;
using System.Threading.Tasks;

namespace Macro.Infrastructure.Controller
{
    internal static class CoroutineRunner
    {
        private static CoroutineHandler _coroutineHandler = new CoroutineHandler();

        private static bool _stopCalled;
        static CoroutineRunner()
        {
            Task.Run(async () =>
            {
                while (!_stopCalled)
                {
                    _coroutineHandler.UpdateCoroutines(0.033F);
                    await Task.Delay(33);
                }
            });
        }

        public static CoroutineHandle Start(IEnumerator enumerator)
        {
            return _coroutineHandler.Start(enumerator);
        }
        public static bool Stop(CoroutineHandle coroutineHandle)
        {
            return _coroutineHandler.Stop(coroutineHandle);
        }
        public static void Dispose()
        {
            _stopCalled = true;
            _coroutineHandler.StopAll();

        }

    }
}
