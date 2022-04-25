using System.Net;
using Http.Resilience.Internals;

namespace Http.Resilience.Policies
{
    internal class WebExceptionRetryPolicy : ExceptionRetryPolicy<WebException>
    {
        private readonly HttpRetryOptions options;

        public WebExceptionRetryPolicy(HttpRetryOptions options)
        {
            this.options = options;
        }

        protected override bool ShouldRetryOnException(WebException webException)
        {
            if (webException.Response is HttpWebResponse httpWebResponse)
            {
                return this.options.IsRetryableResponse(httpWebResponse);
            }

            if (webException.Status == WebExceptionStatus.ConnectFailure ||
                webException.Status == WebExceptionStatus.ConnectionClosed ||
                webException.Status == WebExceptionStatus.KeepAliveFailure ||
                webException.Status == WebExceptionStatus.NameResolutionFailure ||
                webException.Status == WebExceptionStatus.ReceiveFailure ||
                webException.Status == WebExceptionStatus.SendFailure ||
                webException.Status == WebExceptionStatus.Timeout)
            {
                return true;
            }

            return false;
        }
    }
}