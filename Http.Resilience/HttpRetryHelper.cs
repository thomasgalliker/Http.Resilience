using System;
using System.Net.Http;
using System.Threading.Tasks;
using Http.Resilience.Internals;
using Http.Resilience.Internals.Logging;

namespace Http.Resilience
{
    /// <summary>
    /// HTTP retry helper which can be used to retry failed HTTP calls.
    /// The retry behavior is configurable in <seealso cref="HttpRetryOptions"/>.
    /// </summary>
    public class HttpRetryHelper
    {
        private readonly string instance = Guid.NewGuid().ToString().Substring(0, 5).ToUpperInvariant();
        private readonly HttpRetryOptions options;

        private Func<Exception, bool> canRetryDelegate;

        public HttpRetryOptions Options => this.options;

        /// <summary>
        /// Creates an instance of <seealso cref="HttpRetryHelper"/> with default <seealso cref="HttpRetryOptions"/>.
        /// </summary>
        public HttpRetryHelper()
            : this(HttpRetryOptions.Default)
        {
        }

        /// <summary>
        /// Creates an instance of <seealso cref="HttpRetryHelper"/> with default <seealso cref="HttpRetryOptions"/>
        /// overriding <paramref name="maxRetries"/>.
        /// </summary>
        public HttpRetryHelper(int maxRetries)
            : this(new HttpRetryOptions { MaxRetries = maxRetries })
        {
        }

        /// <summary>
        /// Creates an instance of <seealso cref="HttpRetryHelper"/> with <paramref name="httpRetryOptions"/>.
        /// </summary>
        public HttpRetryHelper(HttpRetryOptions httpRetryOptions)
        {
            this.options = httpRetryOptions;
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
            var currentAttempt = 1;
            var maxAttempts = this.options.MaxRetries + 1;
            var lastStatusCode = 0;
            var lastResult = default(TResult);

            while (true)
            {
                try
                {
                    this.Log($"InvokeAsync (Attempt {currentAttempt}/{maxAttempts})");

                    lastResult = await function();
                    if (lastResult is HttpResponseMessage httpResponseMessage)
                    {
                        lastStatusCode = (int)httpResponseMessage.StatusCode;

                        if (this.options.EnsureSuccessStatusCode)
                        {
                            httpResponseMessage.EnsureSuccessStatusCode();
                        }
                    }

                    return lastResult;
                }
                catch (Exception ex)
                {
                    var remainingAttempts = maxAttempts - currentAttempt;
                    if (remainingAttempts > 0 && (NetworkHelper.IsTransientNetworkException(ex, this.options) || this.canRetryDelegate != null && this.canRetryDelegate(ex)))
                    {
                        await this.SleepAsync(remainingAttempts);
                        currentAttempt++;
                        this.Log($"InvokeAsync --> Retry");
                        continue;
                    }

                    throw;
                }
            }
        }

        private async Task SleepAsync(int remainingAttempts)
        {
            var backoff = this.CalculateBackoff(remainingAttempts);
            this.Log($"SleepAsync waiting for {backoff.TotalSeconds:F3}");
            await Task.Delay(backoff);
        }

        protected virtual TimeSpan CalculateBackoff(int remainingAttempts)
        {
            return BackoffTimerHelper.GetExponentialBackoff(this.options.MaxRetries - remainingAttempts + 1, this.options.MinBackoff, this.options.MaxBackoff, this.options.BackoffCoefficient);
        }

        private void Log(string message)
        {
            Logger.Current.Log(LogLevel.Debug, $"HttpRetryHelper_{this.instance}|{message}");
        }

        /// <summary>
        /// Custom retry decision logic if an exception occurred.
        /// </summary>
        public HttpRetryHelper RetryOnException(Func<Exception, bool> handler)
        {
            this.canRetryDelegate = handler;
            return this;
        }

        /// <summary>
        /// Custom retry decision logic if an exception of type <typeparamref name="TException"/> occurred.
        /// </summary>
        public HttpRetryHelper RetryOnException<TException>(Func<TException, bool> handler) where TException : Exception
        {
            return this.RetryOnException((ex) =>
            {
                if (ex is TException tex)
                {
                    return handler(tex);
                }

                return false;
            });
        }
    }
}
