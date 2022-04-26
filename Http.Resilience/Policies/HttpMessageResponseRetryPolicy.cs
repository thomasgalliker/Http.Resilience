using System.Net.Http;
using Http.Resilience.Internals;

namespace Http.Resilience.Policies
{
    internal class HttpMessageResponseRetryPolicy : RetryPolicy<HttpResponseMessage>
    {
        private readonly HttpRetryOptions httpRetryOptions;

        public HttpMessageResponseRetryPolicy(HttpRetryOptions httpRetryOptions)
        {
            this.httpRetryOptions = httpRetryOptions;
        }

        public override bool ShouldRetry(HttpResponseMessage httpResponseMessage)
        {
            return this.httpRetryOptions.IsRetryableResponse(httpResponseMessage);
        }
    }
}