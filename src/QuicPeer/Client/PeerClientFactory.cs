using Microsoft.Extensions.Options;
using QuicPeer.Options;
using System.Net;
using QuicPeer.Common;

namespace QuicPeer.Client;

public class PeerClientFactory(ILoggerFactory loggerFactory, IOptions<ClientOptions> options, IChecksumProvider checksumProvider) : IPeerClientFactory
{
    public PeerClient CreatePeerClient(IPEndPoint remoteEndpoint)
    {
        return new PeerClient(loggerFactory.CreateLogger<PeerClient>(), options, remoteEndpoint, checksumProvider);
    }
}
