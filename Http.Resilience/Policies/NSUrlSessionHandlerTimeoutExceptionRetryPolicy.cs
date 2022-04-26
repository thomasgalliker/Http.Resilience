using System;
using System.Threading.Tasks;

namespace Http.Resilience.Policies
{
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