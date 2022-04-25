namespace Http.Resilience.Policies
{
    public abstract class RetryPolicy<TParameter> : IRetryPolicy
    {
        public bool ShouldRetry(object parameter)
        {
            if (parameter is TParameter t)
            {
                return this.ShouldRetry(t);
            }

            return false;
        }

        protected abstract bool ShouldRetry(TParameter parameter);
    }
}