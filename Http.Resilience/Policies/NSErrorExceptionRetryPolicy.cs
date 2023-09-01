using System;
using System.Collections.Generic;
using System.Reflection;

namespace Http.Resilience.Policies
{
    /// <summary>
    /// Checks if NSErrorExceptions with certain NSUrlError status codes
    /// are transient failures and need to be retried.
    /// </summary>
    /// <remarks>
    /// There is a lot of discussion about what NSUrlError status codes
    /// need to be considered transient. Many existing retry implementations use 
    /// NSURLError.NotConnectedToInternet, NSURLError.InternationalRoamingOff and NSURLError.DataNotAllowed 
    /// as retry codes, which is probably wrong. All codes which require user interaction
    /// to be resolved, should not participate in automatic http retries.
    /// Follow the discussion here:
    /// https://stackoverflow.com/questions/38461477/after-which-errors-should-network-task-be-restarted
    /// </remarks>
    public class NSErrorExceptionRetryPolicy : ExceptionRetryPolicy<Exception>
    {
        private const BindingFlags PropertyFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy;

        private ICollection<NSUrlError> retryCodes = new NSUrlError[]
        {
            NSUrlError.TimedOut,
            NSUrlError.NetworkConnectionLost,
        };

        /// <summary>
        /// Defines which NSUrlError status codes in NSErrorException
        /// are transient failures and need to be retried.
        /// Default values:
        /// - NSUrlError.TimedOut (-1001)
        /// - NSUrlError.NetworkConnectionLost (-1005)
        /// </summary>
        public ICollection<NSUrlError> RetryCodes
        {
            get => this.retryCodes;
            set => this.retryCodes = value ?? Array.Empty<NSUrlError>();
        }

        protected override bool ShouldRetryOnException(Exception ex)
        {
            if (ex.InnerException is Exception innerException)
            {
                var innerExceptionType = innerException.GetType();
                if (innerExceptionType.FullName == "Foundation.NSErrorException")
                {
                    var domainPropertyInfo = innerExceptionType.GetProperty("Domain", PropertyFlags);
                    var domain = domainPropertyInfo?.GetValue(innerException) as string;
                    if (domain == "NSURLErrorDomain")
                    {
                        var codePropertyInfo = innerExceptionType.GetProperty("Code", PropertyFlags);
                        if (codePropertyInfo != null)
                        {
                            var codePropertyValue = codePropertyInfo.GetValue(innerException);
                            var code = Convert.ToInt32(codePropertyValue);
                            if (this.RetryCodes.Contains((NSUrlError)code))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
    }
}