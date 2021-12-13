using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace Http.Resilience.Tests
{
    public class HttpRetryHelperTests
    {
        private readonly ITestOutputHelper testOutputHelper;

        public HttpRetryHelperTests(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void Invoke_ShouldReturnOK()
        {
            // Arrange
            var httpRetryHelper = new HttpRetryHelper(3);
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            // Act
            var response = httpRetryHelper.Invoke(() => httpResponseMessage);

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }


        [Theory]
        [InlineData(WebExceptionStatus.ConnectFailure)]
        [InlineData(WebExceptionStatus.ConnectionClosed)]
        [InlineData(WebExceptionStatus.KeepAliveFailure)]
        [InlineData(WebExceptionStatus.NameResolutionFailure)]
        [InlineData(WebExceptionStatus.ReceiveFailure)]
        [InlineData(WebExceptionStatus.SendFailure)]
        [InlineData(WebExceptionStatus.Timeout)]
        public void Invoke_ShouldRetryOnWebException_ReceiveFailure(WebExceptionStatus webExceptionStatus)
        {
            // Arrange
            var httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => throw new WebException("Test exception", webExceptionStatus),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var response = httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData(SocketError.Interrupted)]
        [InlineData(SocketError.NetworkDown)]
        [InlineData(SocketError.NetworkUnreachable)]
        [InlineData(SocketError.NetworkReset)]
        [InlineData(SocketError.ConnectionAborted)]
        [InlineData(SocketError.ConnectionReset)]
        [InlineData(SocketError.TimedOut)]
        [InlineData(SocketError.HostDown)]
        [InlineData(SocketError.HostUnreachable)]
        [InlineData(SocketError.TryAgain)]
        public void Invoke_ShouldRetryOnSocketException(SocketError socketError)
        {
            // Arrange
            var httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => throw new SocketException((int)socketError),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var response = httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public void Invoke_ShouldReturnInternalServerError()
        {
            // Arrange
            var httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            Action action = () => httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            var httpRequestException = action.Should().Throw<HttpRequestException>().Which;
            httpRequestException.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task InvokeAsync_WithHttpClient()
        {
            // Arrange
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/qod?language=en";

            var httpRetryHelper = new HttpRetryHelper(3);

            // Act
            var httpResponseMessage = await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));

            // Assert
            httpResponseMessage.Should().NotBeNull();
            httpResponseMessage.Content.Should().NotBeNull();
            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task InvokeAsync_WithHttpClient_RetryOnException()
        {
            // Arrange
            var maxRetries = 3;
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/quote/random?language=en&limit=1";
            var retryOnExceptionHits = 0;

            var httpRetryHelper = new HttpRetryHelper(maxRetries)
                .RetryOnException<HttpRequestException>(ex =>
                {
                    retryOnExceptionHits++;
                    return true;
                });

            // Act
            Func<Task> action = async () => await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));

            // Assert
            var httpRequestException = (await action.Should().ThrowAsync<HttpRequestException>()).Which;
            httpRequestException.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            retryOnExceptionHits.Should().Be(maxRetries);
        }

        [Fact]
        public void ShouldThrowExceptionIfRetryOnExceptionIsCalledMoreThanOnce()
        {
            // Arrange
            var httpRetryHelper = new HttpRetryHelper()
                .RetryOnException<HttpRequestException>(ex => true);

            // Act
            Action action = () => httpRetryHelper.RetryOnException<HttpRequestException>(ex => true);

            // Assert
            var invalidOperationException = action.Should().Throw<InvalidOperationException>().Which;
            invalidOperationException.Message.Should().Be("RetryOnException cannot be called more than once");
        }
    }
}
