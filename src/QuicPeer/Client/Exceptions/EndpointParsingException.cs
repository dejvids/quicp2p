namespace QuicPeer.Client.Exceptions;

internal class EndpointParsingException : PeerClientException
{
    public EndpointParsingException()
    {
    }

    public EndpointParsingException(string? message)
        : base(message)
    { }

    public EndpointParsingException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
