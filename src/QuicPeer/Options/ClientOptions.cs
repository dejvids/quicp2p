namespace QuicPeer.Options;

public class ClientOptions : PeerConnectionOptions
{
    public const string SectionName = "Client";
    public required CertificateOptions ClientCertificate { get; set; }
}
