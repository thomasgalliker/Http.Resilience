using System.Net.Http;

namespace Http.Resilience.Policies
{
    public class HttpMessageResponseRetryPolicy : RetryPolicy<HttpResponseMessage>
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