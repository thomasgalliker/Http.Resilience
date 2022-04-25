namespace Http.Resilience.Policies
{
    public interface IRetryPolicy
    {
        bool ShouldRetry(object parameter);
    }
}