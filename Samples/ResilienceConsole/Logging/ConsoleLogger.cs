using System;
using Http.Resilience.Logging;

namespace ResilienceConsole.Logging
{
    public class ConsoleLogger : ILogger
    {
        public void Log(LogLevel logLevel, string message)
        {
            Console.WriteLine($"{DateTime.UtcNow}|Http.Resilience|{logLevel}|{message}[EOL]");
        }
    }
}