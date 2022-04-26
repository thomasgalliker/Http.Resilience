namespace Http.Resilience.Policies
{
    /// <summary>
    /// Abstraction of a retry policy which checks against generic objects of type <typeparamref name="TParameter"/>.
    /// </summary>
    /// <typeparam name="TParameter">Type which is used to check if a retry is necessary.</typeparam>
    public interface IRetryPolicy<in TParameter> : IRetryPolicy
    {
        /// <summary>
        /// Checks if a retry is necessary.
        /// </summary>
        bool ShouldRetry(TParameter parameter);
    }
}