using System;
using Http.Resilience.Logging;
using Xunit.Abstractions;

namespace Http.Resilience.Tests.Logging
{
    public class TestOutputHelperLogger : ILogger
    {
        private readonly ITestOutputHelper testOutputHelper;

        public TestOutputHelperLogger(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        public void Log(LogLevel level, string message)
        {
            try
            {
                this.testOutputHelper.WriteLine($"{DateTime.UtcNow}|{level}|{message}[EOL]");
            }
            catch (InvalidOperationException)
            {
                // TestOutputHelperLogger throws an InvalidOperationException
                // if it is no longer associated with a test case.
            }
        }
    }
}