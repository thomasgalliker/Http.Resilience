using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Http.Resilience.Internals.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class DebugLogger : ILogger
    {
        public void Log(LogLevel logLevel, string message)
        {
            Debug.WriteLine($"{DateTime.UtcNow}|Http.Resilience|{logLevel}|{message}[EOL]");
        }
    }
}