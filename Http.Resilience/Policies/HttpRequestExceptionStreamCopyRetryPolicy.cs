using System;
using System.Net.Http;

namespace Http.Resilience.Policies
{
    public class HttpRequestExceptionStreamCopyRetryPolicy : ExceptionRetryPolicy<HttpRequestException>
    {
        protected override bool ShouldRetryOnException(HttpRequestException httpRequestException)
        {
            if (httpRequestException.Message == "Error while copying content to a stream." &&
                httpRequestException.InnerException is ObjectDisposedException)
            {
                return true;
            }

            return false;
        }
    }
}