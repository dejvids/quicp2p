using System.Net;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.Client;
using QuicPeer.Common;
using QuicPeer.Options;
using QuicPeer.Tests.Common;

namespace QuicPeer.Tests.Client;

public class PeerClientFactoryTests
{
    [Fact]
    public void should_throw_exception_if_cert_is_not_set()
    {
        var clientFactory = new PeerClientFactory(Substitute.For<IOptions<ClientOptions>>(), 
            Substitute.For<IChecksumProvider>());
        
        Assert.ThrowsAny<Exception>(() => 
            clientFactory.CreatePeerClient(new IPEndPoint(IPAddress.Loopback, 500)));
    }

    [Fact]
    public void should_return_client_if_cert_is_set()
    {
        var clientFactory = new PeerClientFactory(Substitute.For<IOptions<ClientOptions>>(), 
            Substitute.For<IChecksumProvider>());
        
        clientFactory.SetCertificate(Convert.FromBase64String(CertificateTests.Pfx), CertificateTests.Password);
        
        var client = clientFactory.CreatePeerClient(new IPEndPoint(IPAddress.Loopback, 500));
        
        Assert.NotNull(client);
    }
}