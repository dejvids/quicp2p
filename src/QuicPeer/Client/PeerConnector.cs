using QuicPeer.Client.Abstraction;

namespace QuicPeer.Client;

public class PeerConnector(IPeerClientFactory clientFactory) : IPeerConnector
{
    private const int DefaultPort = 501;

    public async Task<IPeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
    {
        var remoteEndpoint = EndpointParser.Parse(endpoint, DefaultPort);
        var client = clientFactory.CreatePeerClient(remoteEndpoint);
        
        await client.RunClientAsync(cancellationToken);
        
        return client;
    }
}
