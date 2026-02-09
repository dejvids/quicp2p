using Microsoft.Extensions.Options;
using QuicPeer.Options;
using System.Net;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common;

namespace QuicPeer.Client;

public class PeerClientFactory(IOptions<ClientOptions> options, IChecksumProvider checksumProvider) : IPeerClientFactory
{
    public IPeerClient CreatePeerClient(IPEndPoint remoteEndpoint)
    {
        return new PeerClient(options, remoteEndpoint, checksumProvider);
    }
}