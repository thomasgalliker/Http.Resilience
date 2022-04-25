using System;

namespace Http.Resilience.Policies
{
    internal class RetryOnExceptionPolicy : RetryPolicyDelegate<Exception>
    {
        public RetryOnExceptionPolicy(Func<Exception, bool> handler)
            : base(handler)
        {
        }
    }
}