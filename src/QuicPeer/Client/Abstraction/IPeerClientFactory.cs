using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Client.Abstraction;

public interface IPeerClientFactory
{
    void SetCertificate(X509Certificate2 certificate);
    IPeerClient CreatePeerClient(IPEndPoint remoteEndpoint);
}
