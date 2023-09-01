using System;

namespace Http.Resilience.Policies
{
    public class RetryPolicyDelegate<TParameter> : RetryPolicy<TParameter>
    {
        private readonly Func<TParameter, bool> handler;

        protected RetryPolicyDelegate(Func<TParameter, bool> handler)
        {
            this.handler = handler;
        }

        public override bool ShouldRetry(TParameter parameter)
        {
            return this.handler(parameter);
        }
    }
}