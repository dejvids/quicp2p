using System.Net;

namespace QuicPeer.Client;

public class PeerConnector(IPeerClientFactory clientFactory, ILogger<PeerConnector> logger)
{
    private readonly ILogger<PeerConnector> _logger = logger;
    private readonly IPeerClientFactory _clientFactory = clientFactory;

    public async Task<PeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
    {
        IPEndPoint remoteEndpoint;
        try
        {
            remoteEndpoint = EndpointParser.Parse(endpoint, 501);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "Failed to parse remote endpoint {Endpoint}", endpoint);
            Console.WriteLine("Invalid endpoint format. Please use IP:Port or Hostname:Port.");
            return null;
        }

        var client = _clientFactory.CreatePeerClient(remoteEndpoint);

        try
        {
            await client.RunClientAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {RemoteEndpoint}", remoteEndpoint);
            return null;
        }

        return client;
    }
}
