using System.Net;

namespace QuicPeer.Client;

public interface IPeerClientFactory
{
    PeerClient CreatePeerClient(IPEndPoint remoteEndpoint);
}
