using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Http.Resilience.Internals
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class VssNetworkHelper
    {
        //
        // Summary:
        //     Heuristic used to determine whether an exception is a transient network failure
        //     that should be retried.
        public static bool IsTransientNetworkException(Exception ex)
        {
            return IsTransientNetworkException(ex, new VssHttpRetryOptions());
        }

        //
        // Summary:
        //     Heuristic used to determine whether an exception is a transient network failure
        //     that should be retried.
        public static bool IsTransientNetworkException(Exception ex, VssHttpRetryOptions options)
        {
            return IsTransientNetworkException(ex, options, out var httpStatusCode, out var webExceptionStatus, out var socketErrorCode, out var winHttpErrorCode, out var curlErrorCode);
        }

        //
        // Summary:
        //     Heuristic used to determine whether an exception is a transient network failure
        //     that should be retried.
        public static bool IsTransientNetworkException(Exception ex, out HttpStatusCode? httpStatusCode, out WebExceptionStatus? webExceptionStatus, out SocketError? socketErrorCode, out WinHttpErrorCode? winHttpErrorCode, out CurlErrorCode? curlErrorCode)
        {
            return IsTransientNetworkException(ex, VssHttpRetryOptions.Default, out httpStatusCode, out webExceptionStatus, out socketErrorCode, out winHttpErrorCode, out curlErrorCode);
        }

        //
        // Summary:
        //     Heuristic used to determine whether an exception is a transient network failure
        //     that should be retried.
        public static bool IsTransientNetworkException(Exception ex, VssHttpRetryOptions options, out HttpStatusCode? httpStatusCode, out WebExceptionStatus? webExceptionStatus, out SocketError? socketErrorCode, out WinHttpErrorCode? winHttpErrorCode, out CurlErrorCode? curlErrorCode)
        {
            httpStatusCode = null;
            webExceptionStatus = null;
            socketErrorCode = null;
            winHttpErrorCode = null;
            curlErrorCode = null;
            while (ex != null)
            {
                if (IsTransientNetworkExceptionHelper(ex, options, out httpStatusCode, out webExceptionStatus, out socketErrorCode, out winHttpErrorCode, out curlErrorCode))
                {
                    return true;
                }

                ex = ex.InnerException;
            }

            return false;
        }

        //
        // Summary:
        //     Helper which checks a particular Exception instance (non-recursive).
        private static bool IsTransientNetworkExceptionHelper(Exception ex, VssHttpRetryOptions options, out HttpStatusCode? httpStatusCode, out WebExceptionStatus? webExceptionStatus, out SocketError? socketErrorCode, out WinHttpErrorCode? winHttpErrorCode, out CurlErrorCode? curlErrorCode)
        {
            httpStatusCode = null;
            webExceptionStatus = null;
            socketErrorCode = null;
            winHttpErrorCode = null;
            curlErrorCode = null;
            if (ex is WebException ex2)
            {
                if (ex2.Response != null && ex2.Response is HttpWebResponse response)
                {
                    var httpWebResponse = response;
                    httpStatusCode = httpWebResponse.StatusCode;
                    if (options.RetryableStatusCodes.Contains(httpWebResponse.StatusCode))
                    {
                        return true;
                    }
                }

                webExceptionStatus = ex2.Status;
                if (ex2.Status == WebExceptionStatus.ConnectFailure || ex2.Status == WebExceptionStatus.ConnectionClosed || ex2.Status == WebExceptionStatus.KeepAliveFailure || ex2.Status == WebExceptionStatus.NameResolutionFailure || ex2.Status == WebExceptionStatus.ReceiveFailure || ex2.Status == WebExceptionStatus.SendFailure || ex2.Status == WebExceptionStatus.Timeout)
                {
                    return true;
                }
            }
            else if (ex is SocketException ex3)
            {
                socketErrorCode = ex3.SocketErrorCode;
                if (ex3.SocketErrorCode == SocketError.Interrupted || ex3.SocketErrorCode == SocketError.NetworkDown || ex3.SocketErrorCode == SocketError.NetworkUnreachable || ex3.SocketErrorCode == SocketError.NetworkReset || ex3.SocketErrorCode == SocketError.ConnectionAborted || ex3.SocketErrorCode == SocketError.ConnectionReset || ex3.SocketErrorCode == SocketError.TimedOut || ex3.SocketErrorCode == SocketError.HostDown || ex3.SocketErrorCode == SocketError.HostUnreachable || ex3.SocketErrorCode == SocketError.TryAgain)
                {
                    return true;
                }
            }
            else if (ex is Win32Exception exception)
            {
                var nativeErrorCode = exception.NativeErrorCode;
                if (nativeErrorCode > 12000 && nativeErrorCode <= 12188)
                {
                    winHttpErrorCode = (WinHttpErrorCode)nativeErrorCode;
                    if (winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_CANNOT_CONNECT || winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_CONNECTION_ERROR || winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_INTERNAL_ERROR || winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_NAME_NOT_RESOLVED || winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_SECURE_FAILURE || winHttpErrorCode == WinHttpErrorCode.ERROR_WINHTTP_TIMEOUT)
                    {
                        return true;
                    }
                }
            }
            else if (ex is IOException)
            {
                if (ex.InnerException != null && ex.InnerException is Win32Exception)
                {
                    var stackTrace = ex.StackTrace;
                    if (stackTrace != null && stackTrace.IndexOf("System.Net.Security._SslStream.StartWriting(", StringComparison.Ordinal) >= 0)
                    {
                        return true;
                    }
                }
                else if (ex.Message.Contains("Unable to read data from the transport connection: The connection was closed"))
                {
                    return true;
                }
            }
            else if (ex.GetType().Name == "CurlException" && ex.HResult > 0 && ex.HResult < 94)
            {
                curlErrorCode = (CurlErrorCode)ex.HResult;
                if (curlErrorCode == CurlErrorCode.CURLE_COULDNT_RESOLVE_PROXY || curlErrorCode == CurlErrorCode.CURLE_COULDNT_RESOLVE_HOST || curlErrorCode == CurlErrorCode.CURLE_COULDNT_CONNECT || curlErrorCode == CurlErrorCode.CURLE_HTTP2 || curlErrorCode == CurlErrorCode.CURLE_PARTIAL_FILE || curlErrorCode == CurlErrorCode.CURLE_WRITE_ERROR || curlErrorCode == CurlErrorCode.CURLE_UPLOAD_FAILED || curlErrorCode == CurlErrorCode.CURLE_READ_ERROR || curlErrorCode == CurlErrorCode.CURLE_OPERATION_TIMEDOUT || curlErrorCode == CurlErrorCode.CURLE_INTERFACE_FAILED || curlErrorCode == CurlErrorCode.CURLE_GOT_NOTHING || curlErrorCode == CurlErrorCode.CURLE_SEND_ERROR || curlErrorCode == CurlErrorCode.CURLE_RECV_ERROR)
                {
                    return true;
                }
            }

            return false;
        }
    }
}