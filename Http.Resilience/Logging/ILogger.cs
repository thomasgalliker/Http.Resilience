using System.ComponentModel;

namespace Http.Resilience.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface ILogger
    {
        void Log(LogLevel logLevel, string message);
    }
}