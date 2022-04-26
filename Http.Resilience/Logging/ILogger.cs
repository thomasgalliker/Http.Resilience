namespace Http.Resilience.Logging
{
    public interface ILogger
    {
        void Log(LogLevel logLevel, string message);
    }
}