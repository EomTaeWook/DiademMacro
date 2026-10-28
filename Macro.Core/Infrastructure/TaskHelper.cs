using Dignus.Log;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Macro.Infrastructure
{
    public class TaskHelper
    {
        public static bool TokenCheckDelay(int millisecondsDelay, CancellationToken token)
        {
            try
            {
                if (millisecondsDelay > 0)
                {
                    Task.Delay(millisecondsDelay, token).GetAwaiter().GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (AggregateException exception)
            {
                if (exception.InnerException is TaskCanceledException)
                {
                    return false;
                }
                if (exception.InnerException is OperationCanceledException)
                {
                    return false;
                }
                LogHelper.Error(exception);
                return false;
            }
            return !token.IsCancellationRequested;
        }
    }
}
