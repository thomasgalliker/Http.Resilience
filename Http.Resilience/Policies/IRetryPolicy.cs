namespace Http.Resilience.Policies
{
    /// <summary>
    /// Abstraction of a retry policy which checks against all object classes.
    /// </summary>
    public interface IRetryPolicy
    {
        /// <summary>
        /// Checks if a retry is necessary.
        /// </summary>
        bool ShouldRetry(object parameter);
    }
}