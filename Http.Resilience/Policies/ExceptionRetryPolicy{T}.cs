using System;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Retry policy which recursively evaluates against <typeparamref name="TException"/>.
    /// </summary>
    /// <typeparam name="TException">The exception type.</typeparam>
    public abstract class ExceptionRetryPolicy<TException> : RetryPolicy<Exception> where TException : Exception
    {
        public override bool ShouldRetry(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            do
            {
                if (ex is TException typedEx && this.ShouldRetryOnException(typedEx))
                {
                    return true;
                }

                ex = ex.InnerException;
            } while (ex != null);

            return false;
        }
        
        protected abstract bool ShouldRetryOnException(TException parameter);
    }
}