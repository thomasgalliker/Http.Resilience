using System;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Retry policy which recursively evaluates exceptions <typeparamref name="TException"/> and inner exceptions
    /// for transient errors.
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
                if (ex is TException exception && this.ShouldRetryOnException(exception))
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