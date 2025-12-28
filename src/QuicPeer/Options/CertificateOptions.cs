using System.Net.Security;

namespace QuicPeer.Options;

public class CertificateOptions
{
    public const string SectionName = "Certificate";
    
    public SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quic-peer");
    public string Path { get; set; } = "peer.pfx";
}
