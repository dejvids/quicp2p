namespace QuicPeer.Client.Exceptions;

public class MetadataException : PeerClientException
{
    public MetadataException()
    {
    }

    public MetadataException(string? message) : base(message)
    {
    }

    public MetadataException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}