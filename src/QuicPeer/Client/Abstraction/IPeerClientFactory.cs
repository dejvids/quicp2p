using System.Net;

namespace QuicPeer.Client.Abstraction;

public interface IPeerClientFactory
{
    void SetCertificate(byte[] certificate, string password);
    IPeerClient CreatePeerClient(IPEndPoint remoteEndpoint, CancellationTokenSource cts);
}
