namespace QuicPeer.Common.Exceptions;

public class DataIntegrityException : Exception
{
    public DataIntegrityException()
    {
    }

    public DataIntegrityException(string? message) 
        : base(message)
    {
    }

    public DataIntegrityException(string? message, Exception? innerException) 
        : base(message, innerException)
    {
    }
}