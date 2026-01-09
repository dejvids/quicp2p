namespace QuicPeer.Client;

public class PeerConnector(IPeerClientFactory clientFactory)
{
    const int DefaultPort = 501;
    private readonly IPeerClientFactory _clientFactory = clientFactory;

    public async Task<PeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
    {
        var remoteEndpoint = EndpointParser.Parse(endpoint, DefaultPort);

        var client = _clientFactory.CreatePeerClient(remoteEndpoint);

        await client.RunClientAsync(cancellationToken);

        return client;
    }
}
