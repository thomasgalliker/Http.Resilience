using System;
using Http.Resilience.Internals;

namespace Http.Resilience.Policies
{
    internal class CurlExceptionRetryPolicy : RetryOnExceptionRecursivePolicy<Exception>
    {
        protected override bool ShouldRetryOnException(Exception ex)
        {
            if (ex.GetType().Name == "CurlException" && ex.HResult > 0 && ex.HResult < 94)
            {
                var curlErrorCode = (CurlErrorCode)ex.HResult;
                if (curlErrorCode == CurlErrorCode.CURLE_COULDNT_RESOLVE_PROXY ||
                    curlErrorCode == CurlErrorCode.CURLE_COULDNT_RESOLVE_HOST ||
                    curlErrorCode == CurlErrorCode.CURLE_COULDNT_CONNECT ||
                    curlErrorCode == CurlErrorCode.CURLE_HTTP2 ||
                    curlErrorCode == CurlErrorCode.CURLE_PARTIAL_FILE ||
                    curlErrorCode == CurlErrorCode.CURLE_WRITE_ERROR ||
                    curlErrorCode == CurlErrorCode.CURLE_UPLOAD_FAILED ||
                    curlErrorCode == CurlErrorCode.CURLE_READ_ERROR ||
                    curlErrorCode == CurlErrorCode.CURLE_OPERATION_TIMEDOUT ||
                    curlErrorCode == CurlErrorCode.CURLE_INTERFACE_FAILED ||
                    curlErrorCode == CurlErrorCode.CURLE_GOT_NOTHING ||
                    curlErrorCode == CurlErrorCode.CURLE_SEND_ERROR ||
                    curlErrorCode == CurlErrorCode.CURLE_RECV_ERROR)
                {
                    return true;
                }
            }
                
            return false;
        }
    }
}