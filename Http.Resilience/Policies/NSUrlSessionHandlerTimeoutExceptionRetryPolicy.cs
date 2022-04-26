using System;
using System.Threading.Tasks;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Check if exception is TimeoutException and caused by NSUrlSessionHandler.
    /// </summary>
    public class NSUrlSessionHandlerTimeoutExceptionRetryPolicy : ExceptionRetryPolicy<TimeoutException>
    {
        protected override bool ShouldRetryOnException(TimeoutException ex)
        {
            if (ex.InnerException is TaskCanceledException innerException && innerException.StackTrace.Contains("NSUrlSessionHandler"))
            {
                return true;
            }
                
            return false;
        }
    }
}