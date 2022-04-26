using System;
using System.Reflection;

namespace Http.Resilience.Policies
{
    public class JavaNetUnknownHostExceptionRetryPolicy : ExceptionRetryPolicy<Exception>
    {
        protected override bool ShouldRetryOnException(Exception ex)
        {
            if (string.Equals(ex.GetType().FullName, "Java.Net.UnknownHostException", StringComparison.InvariantCultureIgnoreCase))
            {
                // android_getaddrinfo failed: EAI_NODATA (No address associated with hostname)
                if (ex.InnerException is Exception innerException && 
                    string.Compare(innerException.Message, "android_getaddrinfo", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    return true;
                }
            }
                
            return false;
        }
    }
}