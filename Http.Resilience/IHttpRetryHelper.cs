using System;
using System.Threading.Tasks;
using Http.Resilience.Internals;

namespace Http.Resilience
{
    public interface IHttpRetryHelper
    {
        HttpRetryOptions Options { get; }

        /// <summary>
        ///     Calls <paramref name="action" /> synchronously.
        /// </summary>
        void Invoke(Action action, string actionName = nameof(Invoke));

        /// <summary>
        ///     Calls <paramref name="function" /> synchronously and returns <typeparamref name="TResult" />.
        /// </summary>
        TResult Invoke<TResult>(Func<TResult> function, string functionName = nameof(Invoke));

        /// <summary>
        ///     Calls <paramref name="function" /> asynchronously.
        /// </summary>
        Task InvokeAsync(Func<Task> function, string functionName = nameof(InvokeAsync));

        /// <summary>
        ///     Calls <paramref name="function" /> asynchronously and returns <typeparamref name="TResult" />.
        /// </summary>
        Task<TResult> InvokeAsync<TResult>(Func<Task<TResult>> function, string functionName = nameof(InvokeAsync));

        /// <summary>
        ///     Custom retry decision logic if an exception occurred.
        /// </summary>
        HttpRetryHelper RetryOnException(Func<Exception, bool> handler);

        /// <summary>
        ///     Custom retry decision logic if an exception of type <typeparamref name="TException" /> occurred.
        /// </summary>
        HttpRetryHelper RetryOnException<TException>(Func<TException, bool> handler) where TException : Exception;
    }
}