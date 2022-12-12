using System;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Http.Resilience.Tests
{
    public class HttpRetryOptionsTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public HttpRetryOptionsTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void ShouldThrowInvalidOperationExceptionIfOptionsIsReadonly()
        {
            // Arrange
            var options = HttpRetryOptions.Default;

            // Act
            Action action = () => options.MaxRetries = 99;

            // Assert
            action.Should().Throw<InvalidOperationException>();
        }
    }
}
