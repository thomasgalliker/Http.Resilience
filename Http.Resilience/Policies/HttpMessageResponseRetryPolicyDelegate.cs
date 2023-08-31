using System;
using System.Net.Http;

namespace Http.Resilience.Policies
{
    public class HttpMessageResponseRetryPolicyDelegate : RetryPolicyDelegate<HttpResponseMessage>
    {
        public HttpMessageResponseRetryPolicyDelegate(Func<HttpResponseMessage, bool> handler)
            : base(handler)
        {
        }
    }
}