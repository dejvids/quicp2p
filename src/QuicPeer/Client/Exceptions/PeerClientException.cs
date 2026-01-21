namespace QuicPeer.Client.Exceptions;

public class PeerClientException : Exception
{
    public PeerClientException()
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
