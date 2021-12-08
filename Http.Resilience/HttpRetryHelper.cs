using System;
using System.Net.Http;
using System.Threading;
using Http.Resilience.Internals;

namespace Http.Resilience
{
    /// <summary>
    /// https://github.com/arutnik/meetapp-mobile/blob/2014bc168635bf11781ba7c392df5fd96cb7dbe3/src/PMA.Mobile.Core/Utility/HttpRetryHelper.cs#L58
    /// </summary>
    public class HttpRetryHelper
    {
        private readonly int maxAttempts;
        private readonly Func<Exception, bool> canRetryDelegate;

        private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1.0);
        private static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(1.0);
        private static readonly TimeSpan DeltaBackoff = TimeSpan.FromSeconds(1.0);

        public int MaxAttempts => this.maxAttempts;

        //
        // Parameters:
        //   maxAttempts:
        //     The total number of attempts to invoke the submitted action with. A value of
        //     1 indicates that no retries will be attempted. Note that this was renamed from
        //     "maxRetries" to match the behavior of the parameter (i.e. maxRetries was previously
        //     behaving like maxAttempts).
        //
        //   canRetryDelegate:
        //     Evaluation function which returns true for a given exception if that exception
        //     is permitted to be retried.
        public HttpRetryHelper(int maxAttempts, Func<Exception, bool> canRetryDelegate = null)
        {
            this.maxAttempts = maxAttempts;
            this.canRetryDelegate = canRetryDelegate;
        }

        public void Invoke(Action action)
        {
            this.Invoke(action, out var _);
        }

        //
        // Parameters:
        //   action:
        public void Invoke(Action action, out int remainingAttempts)
        {
            remainingAttempts = this.maxAttempts;
            while (true)
            {
                try
                {
                    action();
                    return;
                }
                catch (Exception ex)
                {
                    if ((VssNetworkHelper.IsTransientNetworkException(ex) || this.canRetryDelegate != null && this.canRetryDelegate(ex)) && remainingAttempts > 1)
                    {
                        this.Sleep(remainingAttempts);
                        remainingAttempts--;
                        continue;
                    }

                    throw;
                }
            }
        }

        public TResult Invoke<TResult>(Func<TResult> function)
        {
            return this.Invoke(function, out _);
        }

        public TResult Invoke<TResult>(Func<TResult> function, out int remainingAttempts)
        {
            remainingAttempts = this.maxAttempts;
            while (true)
            {
                try
                {
                    var result = function();
                    if (result is HttpResponseMessage httpResponseMessage)
                    {
                        httpResponseMessage.EnsureSuccessStatusCode();
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    if ((VssNetworkHelper.IsTransientNetworkException(ex) || this.canRetryDelegate != null && this.canRetryDelegate(ex)) && remainingAttempts > 1)
                    {
                        this.Sleep(remainingAttempts);
                        remainingAttempts--;
                        continue;
                    }

                    throw;
                }
            }
        }

        protected virtual void Sleep(int remainingAttempts)
        {
            Thread.Sleep(BackoffTimerHelper.GetExponentialBackoff(this.maxAttempts - remainingAttempts + 1, MinBackoff, MaxBackoff, DeltaBackoff));
        }
    }
}