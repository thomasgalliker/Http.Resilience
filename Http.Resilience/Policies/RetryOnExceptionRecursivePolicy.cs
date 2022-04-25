using System;

namespace Http.Resilience.Policies
{
    internal abstract class RetryOnExceptionRecursivePolicy<TException> : RetryPolicy<Exception> where TException : Exception
    {
        protected override bool ShouldRetry(Exception ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex));
            }

            do
            {
                if (ex is TException typedEx && this.ShouldRetryRecursively(typedEx))
                {
                    return true;
                }

                ex = ex.InnerException;
            } while (ex != null);

            return false;
        }
        
        protected abstract bool ShouldRetryRecursively(TException parameter);
    }
}