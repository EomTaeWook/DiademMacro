using Dignus.Log;
using Dignus.Utils.Extensions;
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
                    Task.Delay(millisecondsDelay, token).GetResult();
                }
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (AggregateException ex)
            {
                if (!(ex.InnerException is TaskCanceledException) && !(ex.InnerException is OperationCanceledException))
                {
                    LogHelper.Error(ex.Message);
                }
                return false;
            }
            return !token.IsCancellationRequested;
        }
    }
}
