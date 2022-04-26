using System;

namespace Http.Resilience.Policies
{
    internal class RetryPolicyDelegate<TParameter> : RetryPolicy<TParameter>
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