﻿using System;
using System.Diagnostics;

namespace Http.Resilience.Logging
{
    public class DebugLogger : ILogger
    {
        public void Log(LogLevel logLevel, string message)
        {
            Debug.WriteLine($"{DateTime.UtcNow}|Http.Resilience|{logLevel}|{message}[EOL]");
        }
    }
}