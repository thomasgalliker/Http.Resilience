using System;
using System.Threading;

namespace Http.Resilience.Internals.Logging
{
    public static class Logger
    {
        private static readonly Lazy<ILogger> DefaultLogger = new Lazy<ILogger>(CreateDefaultLogger, LazyThreadSafetyMode.PublicationOnly);
        private static ILogger logger;

        private static ILogger CreateDefaultLogger()
        {
            return new DebugLogger();
        }

        public static void SetLogger(ILogger logger)
        {
            Logger.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public static ILogger Current => logger ?? DefaultLogger.Value;
    }
}