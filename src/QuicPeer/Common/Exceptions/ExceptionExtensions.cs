using System.Net.Quic;

namespace QuicPeer.Common.Exceptions;

public static class ExceptionExtensions
{
    public static bool IsConnectionError(this QuicException quicException) =>
        quicException.QuicError is QuicError.ConnectionAborted or QuicError.ConnectionTimeout
            or QuicError.ConnectionRefused or QuicError.ConnectionIdle;
}