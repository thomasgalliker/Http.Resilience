using System;
using System.Reflection;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Checks if exception is NSErrorException with error code -1009.
    /// </summary>
    public class NSErrorExceptionRetryPolicy : ExceptionRetryPolicy<Exception>
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;
        
        protected override bool ShouldRetryOnException(Exception ex)
        {
            if (ex.InnerException is Exception innerException)
            {
                var innerExceptionType = innerException.GetType();
                if (innerExceptionType.FullName == "Foundation.NSErrorException")
                {
                    var propertyInfoCode = innerExceptionType.GetProperty("Code", Flags);
                    if (propertyInfoCode != null)
                    {
                        var code = propertyInfoCode.GetValue(innerException) as int?;
                        if (code is -1009)
                        {
                            return true;
                        }
                    }    
                }
            }
                
            return false;
        }
    }
}