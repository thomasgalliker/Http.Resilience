using System;
using System.ComponentModel;
using System.Threading;

namespace Http.Resilience.Logging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class Logger
    {
        private static readonly Lazy<ILogger> DefaultLogger = new Lazy<ILogger>(CreateDefaultLogger, LazyThreadSafetyMode.PublicationOnly);
        private static ILogger Instance;

        private static ILogger CreateDefaultLogger()
        {
            return new DebugLogger();
        }

        public static void SetLogger(ILogger logger)
        {
            Logger.Instance = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        internal static ILogger Current => Instance ?? DefaultLogger.Value;
    }
}