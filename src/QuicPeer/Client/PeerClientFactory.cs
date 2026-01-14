using Microsoft.Extensions.Options;
using QuicPeer.Options;
using System.Net;

namespace QuicPeer.Client;

public class PeerClientFactory(IOptions<ClientOptions> options) : IPeerClientFactory
{
    private readonly IOptions<ClientOptions> _options = options;

    public PeerClient CreatePeerClient(IPEndPoint remoteEndpoint)
    {
        return new PeerClient(_options, remoteEndpoint);
    }
}
