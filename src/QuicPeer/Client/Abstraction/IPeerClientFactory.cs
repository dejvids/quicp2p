using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Client.Abstraction;

public interface IPeerClientFactory
{
    void SetCertificate(byte[] certificate, string password);
    IPeerClient CreatePeerClient(IPEndPoint remoteEndpoint);
}
