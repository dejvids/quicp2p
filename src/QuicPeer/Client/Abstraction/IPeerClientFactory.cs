using System.Net;

namespace QuicPeer.Client.Abstraction;

public interface IPeerClientFactory
{
    IPeerClient CreatePeerClient(IPEndPoint remoteEndpoint);
}
