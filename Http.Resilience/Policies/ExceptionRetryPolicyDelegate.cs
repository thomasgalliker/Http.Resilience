using System;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Retry policy which recursively evaluates against <see cref="Exception"/>.
    /// </summary>
    internal class ExceptionRetryPolicyDelegate : RetryPolicyDelegate<Exception>
    {
        public ExceptionRetryPolicyDelegate(Func<Exception, bool> handler)
            : base(handler)
        {
        }
    }
}