using System;
using System.Net.Http;
using System.Threading.Tasks;
using Http.Resilience.Internals;
using Http.Resilience.Logging;

namespace Http.Resilience
{
    /// <summary>
    ///     HTTP retry helper which can be used to retry failed HTTP calls.
    ///     The retry behavior is configurable in <seealso cref="HttpRetryOptions" />.
    /// </summary>
    public class HttpRetryHelper : IHttpRetryHelper
    {
        private readonly string instance = Guid.NewGuid().ToString().Substring(0, 5).ToUpperInvariant();

        private Func<Exception, bool> canRetryDelegate;

        /// <summary>
        ///     Creates an instance of <seealso cref="HttpRetryHelper" /> with default <seealso cref="HttpRetryOptions" />.
        /// </summary>
        public HttpRetryHelper()
            : this(HttpRetryOptions.Default)
        {
        }

        /// <summary>
        ///     Creates an instance of <seealso cref="HttpRetryHelper" /> with default <seealso cref="HttpRetryOptions" />
        ///     overriding <paramref name="maxRetries" />.
        /// </summary>
        public HttpRetryHelper(int maxRetries)
            : this(new HttpRetryOptions { MaxRetries = maxRetries })
        {
        }

        /// <summary>
        ///     Creates an instance of <seealso cref="HttpRetryHelper" /> with <paramref name="httpRetryOptions" />.
        /// </summary>
        public HttpRetryHelper(HttpRetryOptions httpRetryOptions)
        {
            this.Options = httpRetryOptions;
        }

        public HttpRetryOptions Options { get; }

        /// <summary>
        ///     Calls <paramref name="action" /> synchronously.
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
        ///     Calls <paramref name="function" /> synchronously and returns <typeparamref name="TResult" />.
        /// </summary>
        public TResult Invoke<TResult>(Func<TResult> function)
        {
            return AsyncHelper.RunSync(() => this.InvokeAsync(() =>
            {
                return Task.FromResult(function());
            }));
        }

        /// <summary>
        ///     Calls <paramref name="function" /> asynchronously.
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
        ///     Calls <paramref name="function" /> asynchronously and returns <typeparamref name="TResult" />.
        /// </summary>
        public async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> function)
        {
            var currentAttempt = 1;
            var maxAttempts = this.Options.MaxRetries + 1;
            var lastResult = default(TResult);

            while (true)
            {
                var remainingAttempts = CalculateRemainingAttempts(currentAttempt, maxAttempts);

                try
                {
                    this.Log(LogLevel.Debug, $"Starting InvokeAsync... (Attempt {currentAttempt}/{maxAttempts})");

                    lastResult = await function();
                    if (lastResult is HttpResponseMessage httpResponseMessage)
                    {
                        httpResponseMessage.EnsureSuccessStatusCode();
                    }

                    this.Log(currentAttempt <= 1 ? LogLevel.Debug : LogLevel.Info,
                        $"InvokeAsync finished successfully (Attempt {currentAttempt}/{maxAttempts})");
                    return lastResult;
                }
                catch (Exception ex)
                {
                    this.Log(LogLevel.Error,
                        $"InvokeAsync failed with exception (Attempt {currentAttempt}/{maxAttempts})");

                    var lastHttpResponseMessage = lastResult as HttpResponseMessage;
                    if (HasRemainingAttempts(remainingAttempts) &&
                        (NetworkHelper.IsTransientNetworkException(ex, lastHttpResponseMessage, this.Options) ||
                         (this.canRetryDelegate != null && this.canRetryDelegate(ex))))
                    {
                        await this.SleepAsync(remainingAttempts);
                        currentAttempt++;
                        this.Log(LogLevel.Info, $"InvokeAsync --> Retry on {ex.GetType().Name}");
                        continue;
                    }

                    if (lastResult is HttpResponseMessage httpResponseMessage && !this.Options.EnsureSuccessStatusCode)
                    {
                        return lastResult;
                    }

                    throw;
                }
            }
        }

        /// <summary>
        ///     Custom retry decision logic if an exception occurred.
        /// </summary>
        public HttpRetryHelper RetryOnException(Func<Exception, bool> handler)
        {
            if (this.canRetryDelegate != null)
            {
                throw new InvalidOperationException($"{nameof(RetryOnException)} cannot be called more than once");
            }

            this.canRetryDelegate = handler;
            return this;
        }

        /// <summary>
        ///     Custom retry decision logic if an exception of type <typeparamref name="TException" /> occurred.
        /// </summary>
        public HttpRetryHelper RetryOnException<TException>(Func<TException, bool> handler) where TException : Exception
        {
            return this.RetryOnException(ex =>
            {
                if (ex is TException tex)
                {
                    return handler(tex);
                }

                return false;
            });
        }

        private static bool HasRemainingAttempts(int remainingAttempts)
        {
            return remainingAttempts > 0;
        }

        private static int CalculateRemainingAttempts(int currentAttempt, int maxAttempts)
        {
            return maxAttempts - currentAttempt;
        }

        private async Task SleepAsync(int remainingAttempts)
        {
            var backoff = this.CalculateBackoff(remainingAttempts);
            if (backoff > TimeSpan.Zero)
            {
                this.Log(LogLevel.Debug, $"SleepAsync waiting for {backoff.TotalSeconds:F3}s");
                await Task.Delay(backoff);
            }
        }

        protected virtual TimeSpan CalculateBackoff(int remainingAttempts)
        {
            return BackoffTimerHelper.GetExponentialBackoff(this.Options.MaxRetries - remainingAttempts + 1,
                this.Options.MinBackoff, this.Options.MaxBackoff, this.Options.BackoffCoefficient);
        }

        private void Log(LogLevel logLevel, string message)
        {
            Logger.Current.Log(logLevel, $"HttpRetryHelper_{this.instance}|{message}");
        }
    }
}