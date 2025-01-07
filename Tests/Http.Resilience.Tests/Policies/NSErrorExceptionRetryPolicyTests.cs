using System;
using FluentAssertions;
using Foundation;
using Http.Resilience.Policies;
using Xunit;

namespace Http.Resilience.Tests.Policies
{
    public class NSErrorExceptionRetryPolicyTests
    {
        [Fact]
        public void ShouldRetry_IfDomainIsNSURLErrorDomain_AndCodeIsTimedOut()
        {
            // Arrange
            var nsErrorException = new NSErrorException
            {
                Domain = "NSURLErrorDomain",
                Code = $"{(int)NSUrlError.TimedOut}"
            };
            var exception = new Exception("Test message", nsErrorException);

            IRetryPolicy retryPolicy = new NSErrorExceptionRetryPolicy();

            // Act
            var output = retryPolicy.ShouldRetry(exception);

            // Assert
            output.Should().BeTrue();
        }
    }
}