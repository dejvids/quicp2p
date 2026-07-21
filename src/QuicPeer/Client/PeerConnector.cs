using QuicPeer.Client.Abstraction;

namespace QuicPeer.Client;

public class PeerConnector(IPeerClientFactory clientFactory) : IPeerConnector
{
    private const int DefaultPort = 501;

    public async Task<IPeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
    {
        var remoteEndpoint = EndpointParser.Parse(endpoint, DefaultPort);
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        IPeerClient? client = null;
        try
        {
            client = clientFactory.CreatePeerClient(remoteEndpoint, cts);
            await client.RunClientAsync();
            return client;
        }
        catch
        {
            if (client is not null)
            {
                await client.DisposeAsync(); // disposes the cts via PeerClient
            }
            else
            {
                cts.Dispose();
            }

            throw;
        }
    }
}

