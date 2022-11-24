using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Http.Resilience.Extensions;
using Http.Resilience.Internals;
using Http.Resilience.Logging;
using Http.Resilience.Policies;

namespace Http.Resilience
{
    /// <summary>
    ///     HTTP retry helper which can be used to retry failed HTTP calls.
    ///     The retry behavior is configurable in <seealso cref="HttpRetryOptions" />.
    /// </summary>
    public class HttpRetryHelper : IHttpRetryHelper
    {
        private readonly string instance = Guid.NewGuid().ToString().Substring(0, 5).ToUpperInvariant();

        static readonly Lazy<IHttpRetryHelper> Implementation = new Lazy<IHttpRetryHelper>(CreateInstance, LazyThreadSafetyMode.PublicationOnly);

        public static IHttpRetryHelper Current => Implementation.Value;

        private static IHttpRetryHelper CreateInstance()
        {
            return new HttpRetryHelper();
        }

        private readonly IDictionary<Type, ICollection<IRetryPolicy>> retryPolicies =
            new Dictionary<Type, ICollection<IRetryPolicy>>();

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

            this.AddOrUpdateRetryPolicy(new HttpMessageResponseRetryPolicy(this.Options));
            this.AddOrUpdateRetryPolicy(new WebExceptionRetryPolicy(this.Options));
            this.AddOrUpdateRetryPolicy(new SystemNetSocketExceptionRetryPolicy());
            this.AddOrUpdateRetryPolicy(new SystemIOExceptionRetryPolicy());
            this.AddOrUpdateRetryPolicy(new CurlExceptionRetryPolicy());
        }

        public HttpRetryOptions Options { get; }

        public IReadOnlyDictionary<Type, ReadOnlyCollection<IRetryPolicy>> RetryPolicies
        {
            get => new ReadOnlyDictionary<Type, ReadOnlyCollection<IRetryPolicy>>(
                this.retryPolicies.ToDictionary(k => k.Key, v => v.Value.ToList().AsReadOnly()));
        }

        /// <summary>
        ///     Calls <paramref name="action" /> synchronously.
        /// </summary>
        public void Invoke(Action action, string actionName = nameof(Invoke))
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            AsyncHelper.RunSync(() => this.InvokeAsync(() =>
            {
                action();
                return Task.FromResult<object>(null);
            }, actionName));
        }

        /// <summary>
        ///     Calls <paramref name="function" /> synchronously and returns <typeparamref name="TResult" />.
        /// </summary>
        public TResult Invoke<TResult>(Func<TResult> function, string functionName = nameof(Invoke))
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            return AsyncHelper.RunSync(() => this.InvokeAsync(() =>
            {
                return Task.FromResult(function());
            }, functionName));
        }

        /// <summary>
        ///     Calls <paramref name="function" /> asynchronously.
        /// </summary>
        public async Task InvokeAsync(Func<Task> function, string functionName = nameof(InvokeAsync))
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            await this.InvokeAsync<object>(async () =>
            {
                await function();
                return Task.FromResult<object>(null);
            }, functionName);
        }

        /// <summary>
        ///     Calls <paramref name="function" /> asynchronously and returns <typeparamref name="TResult" />.
        /// </summary>
        public async Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> function,
            string functionName = nameof(InvokeAsync))
        {
            if (function == null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentNullException(nameof(functionName));
            }

            var currentAttempt = 1;
            var maxAttempts = this.Options.MaxRetries + 1;
            var lastResult = default(TResult);

            while (true)
            {
                var remainingAttempts = CalculateRemainingAttempts(currentAttempt, maxAttempts);

                try
                {
                    this.Log(LogLevel.Info, $"Starting {functionName}... (Attempt {currentAttempt}/{maxAttempts})");

                    lastResult = await function();
                    if (lastResult is HttpResponseMessage httpResponseMessage)
                    {
                        httpResponseMessage.EnsureSuccessStatusCode();
                    }

                    // Retry can be done based on the returned result
                    var hasRemainingAttempts = HasRemainingAttempts(remainingAttempts);
                    var shouldRetry = hasRemainingAttempts && this.EvaluateRetryPolicies(lastResult);
                    if (shouldRetry)
                    {
                        await this.SleepAsync(remainingAttempts);
                        currentAttempt++;
                        this.Log(LogLevel.Info,
                            $"{functionName} --> Retry on result {lastResult.GetType().GetFormattedClassName()}");
                        continue;
                    }

                    // If no retry is necessary, we log a success message
                    // and return the result
                    this.Log(LogLevel.Info,
                        $"{functionName} finished successfully (Attempt {currentAttempt}/{maxAttempts})");

                    return lastResult;
                }
                catch (Exception ex)
                {
                    // Retry can be done based on the thrown exception
                    var hasRemainingAttempts = HasRemainingAttempts(remainingAttempts);

                    this.Log(hasRemainingAttempts ? LogLevel.Debug : LogLevel.Error,
                        $"{functionName} failed with exception (Attempt {currentAttempt}/{maxAttempts})");

                    var shouldRetry = hasRemainingAttempts &&
                                      (this.EvaluateRetryPolicies(lastResult) || this.EvaluateRetryPolicies(ex));
                    if (shouldRetry)
                    {
                        await this.SleepAsync(remainingAttempts);
                        currentAttempt++;
                        this.Log(LogLevel.Info,
                            $"{functionName} --> Retry on exception {ex.GetType().GetFormattedClassName()}");
                        continue;
                    }

                    if (lastResult is HttpResponseMessage && !this.Options.EnsureSuccessStatusCode)
                    {
                        return lastResult;
                    }

                    throw;
                }
            }
        }

        private bool EvaluateRetryPolicies(object parameter)
        {
            if (parameter == null)
            {
                return false;
            }

            var paramType = parameter.GetType();
            var applicableRetryPolicies = this.retryPolicies
                .Where(p => p.Key.IsAssignableFrom(paramType))
                .SelectMany(p => p.Value)
                .ToList();

            foreach (var retryPolicy in applicableRetryPolicies)
            {
                var shouldRetry = retryPolicy.ShouldRetry(parameter);
                this.Log(shouldRetry ? LogLevel.Info : LogLevel.Debug,
                    $"{retryPolicy.GetType().GetFormattedClassName()}.ShouldRetry({paramType.GetFormattedClassName()}) returned {shouldRetry}");
                if (shouldRetry)
                {
                    return true;
                }
            }

            return false;
        }

        public IHttpRetryHelper AddRetryPolicy<T>(IRetryPolicy<T> retryPolicy)
        {
            this.AddOrUpdateRetryPolicy(retryPolicy);
            return this;
        }

        private void AddOrUpdateRetryPolicy<T>(IRetryPolicy<T> retryPolicy)
        {
            if (retryPolicy == null)
            {
                throw new ArgumentNullException(nameof(retryPolicy));
            }

            if (this.retryPolicies.TryGetValue(typeof(T), out var policies))
            {
                var retryPolicyType = retryPolicy.GetType();
                if (retryPolicyType != typeof(HttpMessageResponseRetryPolicyDelegate) &&
                    retryPolicyType != typeof(ExceptionRetryPolicyDelegate) &&
                    policies.Any(p => p.GetType() == retryPolicyType))
                {
                    throw new InvalidOperationException(
                        $"Retry policy of type {retryPolicyType.GetFormattedClassName()} is already added.");
                }

                policies.Add(retryPolicy);
            }
            else
            {
                this.retryPolicies.Add(typeof(T), new List<IRetryPolicy> { retryPolicy });
            }
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