namespace QuicPeer.Client.Exceptions;

internal class PeerClientException : Exception
{
    public PeerClientException()
        : base()
    {
    }

    public PeerClientException(string? message) : base(message) 
    {
    }

    public PeerClientException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
