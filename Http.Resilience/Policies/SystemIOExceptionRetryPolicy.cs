using System;
using System.ComponentModel;
using System.IO;

namespace Http.Resilience.Policies
{
    internal class SystemIOExceptionRetryPolicy : RetryOnExceptionRecursivePolicy<IOException>
    {
        protected override bool ShouldRetryOnException(IOException ioException)
        {
            if (ioException.InnerException is Win32Exception)
            {
                var stackTrace = ioException.StackTrace;
                if (stackTrace != null && stackTrace.IndexOf("System.Net.Security._SslStream.StartWriting(", StringComparison.Ordinal) >= 0)
                {
                    return true;
                }
            }
            else if (ioException.Message.Contains("Unable to read data from the transport connection: The connection was closed"))
            {
                return true;
            }
                
            return false;
        }
    }
}