using System.Net;

namespace QuicPeer.Client;

public interface IPeerClientFactory
{
    IPeerClient CreatePeerClient(IPEndPoint remoteEndpoint);
}
