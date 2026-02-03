using Microsoft.Extensions.Options;

namespace QuicPeer.Options;

public class ServerOptions : PeerConnectionOptions
{
    public const string SectionName = "Server";
    public int Port { get; set; } = 501;
    public required CertificateOptions ServerCertificate { get; set; }
    public ServerTransferOptions Transfer { get; set; } = new();
    public byte RestartAttempts { get; set; } = 3;
    public uint RestartInterval { get; set; } = 5;
}
