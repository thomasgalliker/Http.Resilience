using System;
using Http.Resilience.Internals.Logging;

namespace ResilienceConsole
{
    public class ConsoleLogger : ILogger
    {
        public void Log(LogLevel logLevel, string message)
        {
            Console.WriteLine($"{DateTime.UtcNow}|Http.Resilience|{logLevel}|{message}[EOL]");
        }
    }
}