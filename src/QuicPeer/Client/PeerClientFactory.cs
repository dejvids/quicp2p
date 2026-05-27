using Microsoft.Extensions.Options;
using QuicPeer.Options;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common;

namespace QuicPeer.Client;

public class PeerClientFactory(IOptions<ClientOptions> options, IChecksumProvider checksumProvider) : IPeerClientFactory
{
    private X509Certificate2? _certificate;

    public IPeerClient CreatePeerClient(IPEndPoint remoteEndpoint, CancellationTokenSource cts)
    {
        if (_certificate is null)
        {
            throw new InvalidOperationException();
        }
        return new PeerClient(options, remoteEndpoint, _certificate, checksumProvider, cts);
    }


    public void SetCertificate(byte[] certificate, string password)
    {
        _certificate = X509CertificateLoader.LoadPkcs12(certificate, password);
    }
}