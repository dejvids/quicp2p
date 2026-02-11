namespace QuicPeer.Client.Abstraction;

public interface IPeerConnector
{
    Task<IPeerClient?> Connect(string endpoint, CancellationToken cancellationToken);
}