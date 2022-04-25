using System;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Retry policy which recursively evaluates against <see cref="Exception"/>.
    /// </summary>
    internal class RetryOnExceptionPolicy : RetryPolicyDelegate<Exception>
    {
        public RetryOnExceptionPolicy(Func<Exception, bool> handler)
            : base(handler)
        {
        }
    }
}