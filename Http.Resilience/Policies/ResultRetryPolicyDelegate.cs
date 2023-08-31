using System;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Retry policy which recursively evaluates against <typeparam name="TResult"/>.
    /// </summary>
    public class ResultRetryPolicyDelegate<TResult> : RetryPolicyDelegate<TResult>
    {
        public ResultRetryPolicyDelegate(Func<TResult, bool> handler)
            : base(handler)
        {
        }
    }
}