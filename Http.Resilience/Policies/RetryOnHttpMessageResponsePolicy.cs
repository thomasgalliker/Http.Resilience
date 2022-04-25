using System;
using System.Net.Http;

namespace Http.Resilience.Policies
{
    internal class RetryOnHttpMessageResponsePolicy : RetryPolicyDelegate<HttpResponseMessage>
    {
        public RetryOnHttpMessageResponsePolicy(Func<HttpResponseMessage, bool> handler)
            : base(handler)
        {
        }
    }
}