using System.Net.Security;

namespace QuicPeer.Options;

public class CertificateOptions
{
    public const string SectionName = "Certificate";
    public string KeyPath => Path.Combine("key", "peer.key");
    public string CertPath => "peer.crt";
    
    public SslApplicationProtocol ApplicationProtocol { get; } = new ("quic-peer");

    public string CommonName { get; init; } = "Peer";
    public TimeSpan Lifespan { get; init; } = TimeSpan.FromDays(3650);//By default is valid for 10 years
}
