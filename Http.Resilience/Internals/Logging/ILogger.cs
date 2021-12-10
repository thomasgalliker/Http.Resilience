using System.ComponentModel;

namespace Http.Resilience.Internals.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal interface ILogger
    {
        void Log(LogLevel logLevel, string message);
    }
}