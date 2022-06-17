// ReSharper disable once CheckNamespace
using System;
using System.Net.Http;
using Http.Resilience.Policies;

// ReSharper disable once CheckNamespace
namespace Http.Resilience
{
    public static class HttpRetryHelperExtensions
    {
        public static IHttpRetryHelper RetryOnResult<TResult>(this IHttpRetryHelper httpRetryHelper, Func<TResult, bool> resultFilter)
        {
            if (resultFilter == null)
            {
                throw new ArgumentNullException(nameof(resultFilter));
            }

            httpRetryHelper.AddRetryPolicy(new ResultRetryPolicyDelegate<TResult>(resultFilter));
            return httpRetryHelper;
        }

        /// <summary>
        ///     Custom retry decision logic if an unsuccessful http response is returned.
        /// </summary>
        public static IHttpRetryHelper RetryOnHttpMessageResponse(this IHttpRetryHelper httpRetryHelper, Func<HttpResponseMessage, bool> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            httpRetryHelper.AddRetryPolicy(new HttpMessageResponseRetryPolicyDelegate(handler));
            return httpRetryHelper;
        }

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
        
        /// <summary>
        ///     Custom retry decision logic if an <see cref="Exception"/> occurred.
        /// </summary>
        public static IHttpRetryHelper RetryOnException(this IHttpRetryHelper httpRetryHelper, Func<Exception, bool> exceptionFilter)
        {
            if (exceptionFilter == null)
            {
                throw new ArgumentNullException(nameof(exceptionFilter));
            }

            httpRetryHelper.AddRetryPolicy(new ExceptionRetryPolicyDelegate(exceptionFilter));
            return httpRetryHelper;
        }
    }
}