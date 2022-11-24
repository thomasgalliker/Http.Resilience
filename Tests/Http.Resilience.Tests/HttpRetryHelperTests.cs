using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;
using Http.Resilience.Internals;
using Http.Resilience.Logging;
using Http.Resilience.Policies;
using Http.Resilience.Tests.Logging;
using Http.Resilience.Tests.TestData;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace Http.Resilience.Tests
{
    public class HttpRetryHelperTests
    {
        public HttpRetryHelperTests(ITestOutputHelper testOutputHelper)
        {
            Logger.SetLogger(new TestOutputHelperLogger(testOutputHelper));
        }

        [Theory]
        [ClassData(typeof(NullArgumentTestData))]
        public void Invoke_ShouldThrowArgumentNullException(Action action, string actionName,
            Type expectedExceptionType)
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper();

            // Act
            var func = () => httpRetryHelper.Invoke(action, actionName);

            // Assert
            var ex = func.Should().Throw<Exception>().Which;
            ex.Should().BeOfType(expectedExceptionType);
        }

        public class NullArgumentTestData : TheoryData<Action, string, Type>
        {
            public NullArgumentTestData()
            {
                this.Add(null, null, typeof(ArgumentNullException));
                this.Add(null, null, typeof(ArgumentNullException));
                this.Add(() => { }, null, typeof(ArgumentNullException));
            }
        }

        [Fact]
        public void Invoke_ShouldReturnOK()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            // Act
            var response = httpRetryHelper.Invoke(() => httpResponseMessage);

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturnOK()
        {
            // Arrange
            var httpRetryHelper = HttpRetryHelper.Current;
            var httpResponseMessage = new HttpResponseMessage(HttpStatusCode.OK);

            // Act
            var response = await httpRetryHelper.InvokeAsync(() => Task.FromResult(httpResponseMessage));

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public void Invoke_ShouldReturnVoid()
        {
            // Arrange
            var numberOfInvokes = 0;
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            // Act
            httpRetryHelper.Invoke(() => { numberOfInvokes++; });

            // Assert
            numberOfInvokes.Should().Be(1);
        }

        [Fact]
        public async Task InvokeAsync_ShouldReturnVoid()
        {
            // Arrange
            var numberOfInvokes = 0;
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            // Act
            await httpRetryHelper.InvokeAsync(() =>
            {
                numberOfInvokes++;
                return Task.CompletedTask;
            });

            // Assert
            numberOfInvokes.Should().Be(1);
        }

        [Theory]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public void Invoke_ShouldRetry_WebExceptionWithRetryableStatusCode(HttpStatusCode httpStatusCode)
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            var httpWebResponseMock = new Mock<HttpWebResponse>();
            httpWebResponseMock.Setup(r => r.StatusCode)
                .Returns(httpStatusCode);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => throw new WebException("Test exception", new Exception("Test exception"),
                    WebExceptionStatus.ProtocolError, httpWebResponseMock.Object),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var response = httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [ClassData(typeof(WebExceptionStatusTestdata))]
        public void Invoke_ShouldRetry_WebException(WebExceptionStatus webExceptionStatus)
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

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

        public class WebExceptionStatusTestdata : TheoryData<WebExceptionStatus>
        {
            public WebExceptionStatusTestdata()
            {
                this.Add(WebExceptionStatus.ConnectFailure);
                this.Add(WebExceptionStatus.ConnectionClosed);
                this.Add(WebExceptionStatus.KeepAliveFailure);
                this.Add(WebExceptionStatus.NameResolutionFailure);
                this.Add(WebExceptionStatus.ReceiveFailure);
                this.Add(WebExceptionStatus.SendFailure);
                this.Add(WebExceptionStatus.Timeout);
            }
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
        public void Invoke_ShouldRetry_SocketException(SocketError socketError)
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => throw new SocketException((int)socketError), () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var response = httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            attempts.Should().BeEmpty();
        }

        [Fact]
        public void Invoke_ShouldRetry_CurlException()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => throw new CurlException { HResult = (int)CurlErrorCode.CURLE_COULDNT_RESOLVE_HOST },
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var response = httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            attempts.Should().BeEmpty();
        }

        public class CurlException : Exception
        {
        }

        [Fact]
        public void Invoke_ShouldRetry_IOExceptionWithWin32Exception()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => throw new TestIOException(
                    message: "Test IO Exception",
                    stackTrace: $"Object name: 'System.Net.Sockets.NetworkStream'.{Environment.NewLine}" +
                                $"at System.Net.Sockets.NetworkStream.EndWrite(IAsyncResult asyncResult){Environment.NewLine}" +
                                $"at System.Net.Security._SslStream.StartWriting(Byte[] buffer, Int32 offset, Int32 count, AsyncProtocolRequest asyncRequest){Environment.NewLine}" +
                                $"at System.Net.Security._SslStream.ProcessWrite(Byte[] buffer, Int32 offset, Int32 count, AsyncProtocolRequest asyncRequest){Environment.NewLine}",
                    innerException: new Win32Exception("Test Win32 Exception")),
                () => throw new TestIOException(
                    message: "Unable to read data from the transport connection: The connection was closed",
                    stackTrace: "",
                    innerException: new Exception("Test Exception")),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var response = httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            attempts.Should().BeEmpty();
        }

        /// <summary>
        /// Exception just for testing purposes.
        /// </summary>
        public class TestIOException : IOException
        {
            public TestIOException(string message, string stackTrace, Exception innerException)
                : base(message, innerException)
            {
                this.StackTrace = stackTrace;
            }

            public override string StackTrace { get; }
        }

        [Theory]
        [InlineData(HttpStatusCode.BadGateway)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.GatewayTimeout)]
        public void Invoke_ShouldRetry_OnRetryableStatusCode(HttpStatusCode httpStatusCode)
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => new HttpResponseMessage(httpStatusCode), () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var response = httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            response.Should().NotBeNull();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            attempts.Should().BeEmpty();
        }

        [Fact]
        public void Invoke_ShouldNotRetry_OnUnsuccessfulStatusCode()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

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
            attempts.Should().HaveCount(1);
        }

        [Fact]
        public void Invoke_ShouldNotRetry_EnsureSuccessStatusCode_Enabled()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);
            httpRetryHelper.Options.EnsureSuccessStatusCode = true;

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var action = () => httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            var httpRequestException = action.Should().Throw<HttpRequestException>().Which;
            httpRequestException.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            attempts.Should().HaveCount(1);
        }

        [Fact]
        public void Invoke_ShouldNotRetry_EnsureSuccessStatusCode_Disabled()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);
            httpRetryHelper.Options.EnsureSuccessStatusCode = false;

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => new HttpResponseMessage(HttpStatusCode.InternalServerError),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var action = () => httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            var response = action.Should().NotThrow().Which;
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            attempts.Should().HaveCount(1);
        }

        [Fact]
        public async Task Invoke_ShouldNotRetry_IfHasRetryableStatusCodeButFilterIsActive_HttpRequestException()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => CreateHttpResponseMessage_WithHostOfflineErrorHeaders(HttpStatusCode.ServiceUnavailable),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            var action = () => httpRetryHelper.InvokeAsync(() => Task.FromResult(attempts.Dequeue()()));

            // Assert
            var httpRequestException = (await action.Should().ThrowAsync<HttpRequestException>()).Which;
            httpRequestException.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            attempts.Should().HaveCount(1);
        }

        [Fact]
        public void Invoke_ShouldNotRetry_IfHasRetryableStatusCodeButFilterIsActive_WebException()
        {
            // Arrange
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(3);
            var httpWebResponse = CreateHttpWebResponse_WithHostOfflineErrorHeaders(HttpStatusCode.ServiceUnavailable);

            var attempts = new Queue<Func<HttpResponseMessage>>(new List<Func<HttpResponseMessage>>
            {
                () => throw new WebException("Test exception", null, WebExceptionStatus.ProtocolError, httpWebResponse),
                () => new HttpResponseMessage(HttpStatusCode.OK),
            });

            // Act
            Action action = () => httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            var httpRequestException = action.Should().Throw<WebException>().Which;
            httpRequestException.Response.Should().BeAssignableTo<HttpWebResponse>();
            attempts.Should().HaveCount(1);
        }

        private static HttpResponseMessage CreateHttpResponseMessage_WithHostOfflineErrorHeaders(
            HttpStatusCode httpStatusCode)
        {
            var httpResponseMessage = new HttpResponseMessage(httpStatusCode);
            httpResponseMessage.Headers.Add("key1", new List<string> { "value1", "value2" });
            httpResponseMessage.Headers.Add("X-VSS-HostOfflineError", (string)null);
            return httpResponseMessage;
        }

        private static HttpWebResponse CreateHttpWebResponse_WithHostOfflineErrorHeaders(HttpStatusCode httpStatusCode)
        {
            var httpWebResponseMock = new Mock<HttpWebResponse>();
            httpWebResponseMock.Setup(r => r.StatusCode)
                .Returns(httpStatusCode);

            httpWebResponseMock.SetupGet(x => x.Headers)
                .Returns(new WebHeaderCollection
                {
                    { "key1", "value1" }, { "key1", "value2" }, { "X-VSS-HostOfflineError", null }
                });

            httpWebResponseMock.Setup(x => x.GetResponseStream())
                .Returns(new MemoryStream(new byte[] { }));

            return httpWebResponseMock.Object;
        }

        [Fact]
        public async Task InvokeAsync_WithHttpClient()
        {
            // Arrange
            const int maxRetries = 3;
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/qod?language=en";

            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(maxRetries);

            // Act
            var httpResponseMessage =
                await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));

            // Assert
            httpResponseMessage.Should().NotBeNull();
            httpResponseMessage.Content.Should().NotBeNull();
            httpResponseMessage.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task InvokeAsync_WithHttpClient_RetryOnException()
        {
            // Arrange
            const int maxRetries = 3;
            var httpClient = new HttpClient();
            var requestUri = "https://quotes.rest/quote/random?language=en&limit=1";
            var retryOnExceptionHits = 0;

            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(maxRetries);
            httpRetryHelper.RetryOnException<HttpRequestException>(ex =>
            {
                retryOnExceptionHits++;
                return true;
            });
            httpRetryHelper.RetryOnException<WebException>(ex =>
            {
                retryOnExceptionHits++;
                return true;
            });
            httpRetryHelper.RetryOnHttpMessageResponse(m =>
            {
                retryOnExceptionHits++;
                return true;
            });

            // Act
            Func<Task> action = async () =>
                await httpRetryHelper.InvokeAsync(async () => await httpClient.GetAsync(requestUri));

            // Assert
            var httpRequestException = (await action.Should().ThrowAsync<HttpRequestException>()).Which;
            httpRequestException.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            retryOnExceptionHits.Should().Be(maxRetries);
        }

        [Fact]
        public void Invoke_RetryOnResult()
        {
            // Arrange
            const int maxRetries = 3;
            var retryHits = 0;

            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(maxRetries);
            httpRetryHelper.RetryOnResult<ServiceResult>(r =>
            {
                if (r.ErrorMessage == "invalid_grant")
                {
                    return false;
                }

                retryHits++;
                return true;
            });

            var attempts = new Queue<Func<ServiceResult>>(new List<Func<ServiceResult>>
            {
                () => new ServiceResult { ErrorMessage = "error" },
                () => new ServiceResult { ErrorMessage = "invalid_grant" },
                () => new ServiceResult { ErrorMessage = null }
            });

            // Act
            Action action = () => httpRetryHelper.Invoke(() => attempts.Dequeue()());

            // Assert
            action.Should().NotThrow();
            retryHits.Should().Be(1);
        }

        [Fact]
        public void AddRetryPolicy_Success()
        {
            // Arrange
            const int maxRetries = 3;
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(maxRetries)
                .AddRetryPolicy(new NSErrorExceptionRetryPolicy())
                .AddRetryPolicy(new NSUrlSessionHandlerTimeoutExceptionRetryPolicy());

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
            attempts.Should().HaveCount(1);
        }

        [Fact]
        public void AddRetryPolicy_FailsIfAlreadyAdded()
        {
            // Arrange
            const int maxRetries = 3;
            IHttpRetryHelper httpRetryHelper = new HttpRetryHelper(maxRetries);
            httpRetryHelper.AddRetryPolicy(new JavaNetUnknownHostExceptionRetryPolicy());

            // Act
            Action action = () => httpRetryHelper.AddRetryPolicy(new JavaNetUnknownHostExceptionRetryPolicy());

            // Assert
            var invalidOperationException = action.Should().Throw<InvalidOperationException>().Which;
            invalidOperationException.Message.Should()
                .Contain("Retry policy of type JavaNetUnknownHostExceptionRetryPolicy is already added");
        }
    }
}