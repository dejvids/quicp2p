namespace QuicPeer.Options;

public class ClientOptions : PeerConnectionOptions
{
    public const string SectionName = "Client";
    public required CertificateOptions ClientCertificate { get; set; }
    public TransferOptions Transfer { get; set; } = new();
    
    /// <summary>
    /// Delay in ms to wait until server finish TLS handshake.
    /// It is needed to detect rejected connection on client's side.
    /// </summary>
    public int TlsHandshakeDelay { get; set; } = 10;
}
