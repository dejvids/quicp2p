using Microsoft.Extensions.Options;
using QuicPeer.Options;
using System.Net;

namespace QuicPeer.Client;

public class PeerClientFactory(ILoggerFactory loggerFactory, IOptions<ClientOptions> options) : IPeerClientFactory
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IOptions<ClientOptions> _options = options;

    public PeerClient CreatePeerClient(IPEndPoint remoteEndpoint)
    {
        return new PeerClient(_loggerFactory.CreateLogger<PeerClient>(), _options, remoteEndpoint);
    }
}
