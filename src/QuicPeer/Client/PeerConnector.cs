namespace QuicPeer.Client;

public class PeerConnector(IPeerClientFactory clientFactory)
{
    const int DefaultPort = 501;
    private readonly IPeerClientFactory _clientFactory = clientFactory;

    public virtual async Task<IPeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
    {
        var remoteEndpoint = EndpointParser.Parse(endpoint, DefaultPort);

        var client = _clientFactory.CreatePeerClient(remoteEndpoint);

        await client.RunClientAsync(cancellationToken);

        return client;
    }
}
