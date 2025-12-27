
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Server;

public abstract class ServerBase
{
    private const string CertPath = "peer.pfx";
    protected abstract SslApplicationProtocol ApplicationProtocol { get; }
    protected virtual int Port => 501;
    public async Task RunServerAsync()
    {
        try
        {
            EnsureProtocolSupport();
            var serverCertificate = await LoadServerCertificate();
            var options = BootstrapServer(serverCertificate);
            await RunServerInternal(options);
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private QuicListenerOptions BootstrapServer(X509Certificate2 serverCertificate)
    {
        return new QuicListenerOptions
        {
            ApplicationProtocols = [ApplicationProtocol],
            ListenEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, Port),

            ConnectionOptionsCallback = async (_, _, _) => await GetConnectionOptionsAsync(serverCertificate)
        };
    }

    protected virtual ValueTask<QuicServerConnectionOptions> GetConnectionOptionsAsync(X509Certificate2 serverCertificate)
    {
        QuicServerConnectionOptions connectionOptions = new()
        {
            DefaultStreamErrorCode = 0xA,
            DefaultCloseErrorCode = 0xB,
            ServerAuthenticationOptions = new SslServerAuthenticationOptions
            {
                ApplicationProtocols = [ApplicationProtocol],
                ServerCertificate = serverCertificate
            },
            IdleTimeout = TimeSpan.FromSeconds(30),
            MaxInboundUnidirectionalStreams = 10,
            MaxInboundBidirectionalStreams = 10
        };

        return ValueTask.FromResult(connectionOptions);
    }

    protected abstract Task RunServerInternal(QuicListenerOptions options);

    private static void EnsureProtocolSupport()
    {
        if (!QuicListener.IsSupported)
        {
            throw new NotSupportedException("QuicListener is not supported");
        }

        if (!QuicConnection.IsSupported)
        {
            throw new NotSupportedException("QuicConnection is not supported");
        }
    }

    private static async ValueTask<X509Certificate2> LoadServerCertificate()
    {
        if (!File.Exists(CertPath))
        {
            await CreateSelfSignedCertficate(CertPath);
            Console.WriteLine($"Created Self-Signed certificate {CertPath}");
        }

        Console.WriteLine("Loading Self-Signed certificate from file");
        return X509CertificateLoader.LoadPkcs12FromFile(CertPath, string.Empty);
    }

    private static async Task CreateSelfSignedCertficate(string certPath)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var distinguishedName = new X500DistinguishedName("CN=QuicPeer");

        var request = new CertificateRequest(
            distinguishedName,
            ecdsa,
            HashAlgorithmName.SHA256);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment |
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature,
                critical: false));

        var notAfter = DateTimeOffset.UtcNow.AddYears(1);
        var notBefore = DateTimeOffset.UtcNow.AddDays(-1);

        var certificate = request.CreateSelfSigned(notBefore, notAfter);

        var bytes = certificate.Export(X509ContentType.Pfx);

        await File.WriteAllBytesAsync(certPath, bytes);
    }

}

