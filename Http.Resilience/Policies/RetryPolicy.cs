namespace Http.Resilience.Policies
{
    /// <summary>
    /// Retry policy which checks against generic objects of type <typeparamref name="TParameter"/>.
    /// </summary>
    public abstract class RetryPolicy<TParameter> : IRetryPolicy<TParameter>
    {
        public bool ShouldRetry(object parameter)
        {
            if (parameter is TParameter t)
            {
                return this.ShouldRetry(t);
            }

            return false;
        }

        public abstract bool ShouldRetry(TParameter parameter);
    }
}