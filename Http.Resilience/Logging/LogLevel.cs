using System.ComponentModel;

namespace Http.Resilience.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum LogLevel
    {
        Info,
        Warning,
        Debug,
        Error,
    }
}