using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Server;

public abstract class ServerBase(IOptions<ServerOptions> serverOptions, ILogger logger) : BackgroundService
{
    protected ILogger Logger { get; } = logger;
    protected ServerOptions Options { get; } = serverOptions.Value;

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping server");

        return base.StopAsync(cancellationToken);
    }

    protected async Task RunServerAsync()
    {
        try
        {
            EnsureProtocolSupport();
            var serverCertificate = await LoadServerCertificate(Options.ServerCertificate);
            var options = BootstrapServer(serverCertificate);
            await RunServerInternal(options);
        }
        catch (NotSupportedException ex)
        {
            Logger.LogError(ex, "msquic is not available.");
        }
    }

    private QuicListenerOptions BootstrapServer(X509Certificate2 serverCertificate)
    {
        return new QuicListenerOptions
        {
            ApplicationProtocols = [Options.ServerCertificate.ApplicationProtocol],
            ListenEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Any, Options.Port),

            ConnectionOptionsCallback = async (_, _, _) => await GetConnectionOptionsAsync(serverCertificate)
        };
    }

    protected virtual ValueTask<QuicServerConnectionOptions> GetConnectionOptionsAsync(
        X509Certificate2 serverCertificate)
    {
        QuicServerConnectionOptions connectionOptions = new()
        {
            DefaultStreamErrorCode = Options.DefaultStreamErrorCode,
            DefaultCloseErrorCode = Options.DefaultCloseErrorCode,
            ServerAuthenticationOptions = new SslServerAuthenticationOptions
            {
                ApplicationProtocols = [Options.ServerCertificate.ApplicationProtocol],
                ServerCertificate = serverCertificate
            },
            IdleTimeout = TimeSpan.FromSeconds(Options.IdleTimeout),
            MaxInboundUnidirectionalStreams = Options.MaxInboundUnidirectionalStreams,
            MaxInboundBidirectionalStreams = Options.MaxInboundBidirectionalStreams
        };

        AddClientAuthentication(connectionOptions);
        return ValueTask.FromResult(connectionOptions);
    }

    private void AddClientAuthentication(QuicServerConnectionOptions connectionOptions)
    {
        connectionOptions.ServerAuthenticationOptions.ClientCertificateRequired = true;
        connectionOptions.ServerAuthenticationOptions.RemoteCertificateValidationCallback =
            (_, certificate, _, sslPolicyErrors) =>
            {
                if (certificate is not null)
                {
                    Logger.LogInformation("Client certificate received.");
                    //Accept any certificate for poc purposes.
                    return true;
                }

                if (sslPolicyErrors != SslPolicyErrors.None)
                {
                    Logger.LogError("SSL Policy Errors: {Errors}", sslPolicyErrors);
                    return false;
                }

                return true;
            };
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

    private async ValueTask<X509Certificate2> LoadServerCertificate(CertificateOptions certificateOptions)
    {
        if (!File.Exists(certificateOptions.Path))
        {
            await CreateSelfSignedCertficate(certificateOptions.Path);
            Logger.LogInformation("Created Self-Signed certificate {Path}", certificateOptions.Path);
        }

        Logger.LogInformation("Loading Self-Signed certificate from file");
        return X509CertificateLoader.LoadPkcs12FromFile(certificateOptions.Path, string.Empty);
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