using System;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Retry policy which recursively evaluates exceptions and inner exceptions
    /// for transient failures.
    /// </summary>
    public class ExceptionRetryPolicyDelegate : RetryPolicyDelegate<Exception>
    {
        public ExceptionRetryPolicyDelegate(Func<Exception, bool> handler)
            : base(handler)
        {
        }
    }
}