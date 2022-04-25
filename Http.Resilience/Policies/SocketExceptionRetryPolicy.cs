using System.Net.Sockets;

namespace Http.Resilience.Policies
{
    internal class SocketExceptionRetryPolicy : ExceptionRetryPolicy<SocketException>
    {
        protected override bool ShouldRetryOnException(SocketException socketException)
        {
            if (socketException.SocketErrorCode == SocketError.Interrupted ||
                socketException.SocketErrorCode == SocketError.NetworkDown ||
                socketException.SocketErrorCode == SocketError.NetworkUnreachable ||
                socketException.SocketErrorCode == SocketError.NetworkReset ||
                socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                socketException.SocketErrorCode == SocketError.ConnectionReset ||
                socketException.SocketErrorCode == SocketError.TimedOut ||
                socketException.SocketErrorCode == SocketError.HostDown ||
                socketException.SocketErrorCode == SocketError.HostUnreachable ||
                socketException.SocketErrorCode == SocketError.TryAgain)
            {
                return true;
            }

            return false;
        }
    }
}