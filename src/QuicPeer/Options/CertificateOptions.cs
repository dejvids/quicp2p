using System.Net.Security;

namespace QuicPeer.Options;

public class CertificateOptions
{
    public const string SectionName = "Certificate";
    
    public SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quic-peer");
    public string Path { get; init; } = "peer.pfx";
    public string CommonName { get; init; } = "Peer";
    public TimeSpan Lifetime { get; init; } = TimeSpan.FromDays(3650);//By default is valid for 10 years
}
