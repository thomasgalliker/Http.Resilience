// ReSharper disable once CheckNamespace
using System;

namespace Http.Resilience
{
    public static class HttpRetryHelperExtensions
    {
        /// <summary>
        ///     Retries if any exception occurred.
        /// </summary>
        public static IHttpRetryHelper RetryOnException(this IHttpRetryHelper httpRetryHelper)
        {
            return httpRetryHelper.RetryOnException(_ => true);
        }
        
        /// <summary>
        ///     Custom retry decision logic if an exception of type <typeparamref name="TException" /> occurred.
        /// </summary>
        public static IHttpRetryHelper RetryOnException<TException>(this IHttpRetryHelper httpRetryHelper, Func<TException, bool> exceptionFilter) where TException : Exception
        {
            return httpRetryHelper.RetryOnException(ex =>
            {
                if (ex is TException tex)
                {
                    return exceptionFilter(tex);
                }

                return false;
            });
        }
    }
}