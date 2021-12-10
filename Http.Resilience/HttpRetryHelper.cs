using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Http.Resilience.Internals;

namespace Http.Resilience
{
    /// <summary>
    /// https://github.com/arutnik/meetapp-mobile/blob/2014bc168635bf11781ba7c392df5fd96cb7dbe3/src/PMA.Mobile.Core/Utility/HttpRetryHelper.cs#L58
    /// </summary>
    public class HttpRetryHelper
    {
        private readonly string _instance = Guid.NewGuid().ToString().Substring(0, 5).ToUpperInvariant();
        private readonly int maxAttempts;
        private readonly Func<Exception, bool> canRetryDelegate;

        private IDictionary<int, Func<Exception, Task<object>>> statusCodeExceptionHandlers = new Dictionary<int, Func<Exception, Task<object>>>();

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

        /// <summary>
        /// Calls <paramref name="action"/> synchronously.
        /// </summary>
        public void Invoke(Action action)
        {
            AsyncHelper.RunSync(() => this.InvokeAsync(() =>
            {
                action();
                return Task.FromResult<object>(null);
            }));
        }

        /// <summary>
        /// Calls <paramref name="function"/> synchronously and returns <typeparamref name="TResult"/>.
        /// </summary>
        public TResult Invoke<TResult>(Func<TResult> function)
        {
            return AsyncHelper.RunSync(() => this.InvokeAsync(() =>
            {
                return Task.FromResult(function());
            }));
        }

        /// <summary>
        /// Calls <paramref name="function"/> asynchronously.
        /// </summary>
        public async Task InvokeAsync(Func<Task> function)
        {
            await this.InvokeAsync<object>(async () =>
            {
                await function();
                return Task.FromResult<object>(null);
            });
        }

        /// <summary>
        /// Calls <paramref name="function"/> asynchronously and returns <typeparamref name="TResult"/>.
        /// </summary>
        public async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> function)
        {
            var remainingAttempts = maxAttempts;
            var lastStatusCode = 0;
            var lastResult = default(TResult);

            while (true)
            {
                try
                {
                    Log($"InvokeAsync (Attempt {maxAttempts - remainingAttempts} / {maxAttempts})");

                    lastResult = await function();
                    if (lastResult is HttpResponseMessage httpResponseMessage)
                    {
                        lastStatusCode = (int)httpResponseMessage.StatusCode;
                        httpResponseMessage.EnsureSuccessStatusCode();
                    }

                    return lastResult;
                }
                catch (Exception ex)
                {
                    if (remainingAttempts > 1 && (VssNetworkHelper.IsTransientNetworkException(ex) || canRetryDelegate != null && canRetryDelegate(ex)))
                    {
                        await SleepAsync(remainingAttempts);
                        remainingAttempts--;
                        Log($"InvokeAsync --> Retry");
                        continue;
                    }

                    //if (ex is WebException webException)
                    //{
                    //    if (webException.Response is HttpWebResponse response)
                    //    {
                    //        lastStatusCode = (int)response.StatusCode;
                    //    }
                    //}
                    //if (statusCodeExceptionHandlers.TryGetValue(lastStatusCode, out var statusCodeExceptionHandler))
                    //{
                    //    Log($"InvokeAsync --> custom error handling for status code {lastStatusCode}");
                    //    return await statusCodeExceptionHandler(ex) as TResult;
                    //}

                    throw;
                }
            }
        }

        private async Task SleepAsync(int remainingAttempts)
        {
            var backoff = CalculateBackoff(remainingAttempts);
            await Task.Delay(backoff);
        }

        protected virtual TimeSpan CalculateBackoff(int remainingAttempts)
        {
            return BackoffTimerHelper.GetExponentialBackoff(maxAttempts - remainingAttempts + 1, MinBackoff, MaxBackoff, DeltaBackoff);
        }

        private void Log(string message)
        {
            Debug.WriteLine($"HttpRetryHelper_{_instance}|{message}");
        }

        public void OnCodeAsync(int statusCode, Func<Exception, Task<object>> handler)
        {
            this.statusCodeExceptionHandlers[statusCode] = handler;
        }
    }

    internal static class AsyncHelper
    {
        private static readonly TaskFactory _myTaskFactory = new TaskFactory(
            CancellationToken.None,
            TaskCreationOptions.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);

        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
        {
            return _myTaskFactory
              .StartNew(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }

        public static void RunSync(Func<Task> func)
        {
            _myTaskFactory
              .StartNew(func)
              .Unwrap()
              .GetAwaiter()
              .GetResult();
        }
    }
}
