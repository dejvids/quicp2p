using System.IO.Abstractions;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Options;

namespace QuicPeer.Server;

public abstract class ServerBase(
    IOptions<ServerOptions> serverOptions,
    ILogger logger,
    IPeersStore peersStore,
    IFileSystem fileSystem) : BackgroundService
{
    protected ILogger Logger { get; } = logger;
    protected ServerOptions Options { get; } = serverOptions.Value;

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping server...");

        return base.StopAsync(cancellationToken);
    }

    protected async Task RunServerAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Starting server...");
        try
        {
            EnsureProtocolSupport();
            var serverCertificate = await LoadServerCertificate(Options.ServerCertificate);
            var options = BootstrapServer(serverCertificate);
            await RunServerInternal(options, stoppingToken);
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
            MaxInboundBidirectionalStreams = Options.MaxInboundBidirectionalStreams,
            KeepAliveInterval = TimeSpan.FromSeconds(Options.KeepAliveInterval)
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
                if (!Options.RequireClientCertificate)
                {
                    return true;
                }

                if (certificate is null)
                {
                    return false;
                }

                var remoteCertificate = new Certificate(certificate);

                return peersStore.Contains(remoteCertificate) && 
                       !remoteCertificate.IsExpired(TimeProvider.System) &&
                       (!Options.ValidateFullChain || sslPolicyErrors == SslPolicyErrors.None);
            };
    }

    protected abstract Task RunServerInternal(QuicListenerOptions options, CancellationToken stoppingToken);

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
        if (!fileSystem.File.Exists(CertificateOptions.Path))
        {
            await CreateSelfSignedCertificate();
            Logger.LogInformation("Created Self-Signed certificate {Path}", CertificateOptions.Path);
        }

        Logger.LogInformation("Loading Self-Signed certificate from file");
        return X509CertificateLoader.LoadPkcs12FromFile(CertificateOptions.Path, string.Empty);
    }

    private async Task CreateSelfSignedCertificate()
    {
        var certificate = new Certificate(Options.ServerCertificate, TimeProvider.System);
        var pfx = certificate.GetBytes();
        await fileSystem.File.WriteAllBytesAsync(CertificateOptions.Path, pfx);
    }
}