namespace Http.Resilience.Internals.Logging
{
    public interface ILogger
    {
        void Log(LogLevel logLevel, string message);
    }
}