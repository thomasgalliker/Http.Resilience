using System.ComponentModel;

namespace Http.Resilience.Internals.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal enum LogLevel
    {
        Info,
        Warning,
        Debug,
        Error,
    }
}