using System.Net;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Checks for <see cref="WebException"/> and the associated <see cref="HttpWebResponse"/> (if available)
    /// or if the returned <see cref="WebExceptionStatus"/> is a transient failure.
    /// </summary>
    public class WebExceptionRetryPolicy : ExceptionRetryPolicy<WebException>
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