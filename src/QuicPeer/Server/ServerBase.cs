using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Options;

namespace QuicPeer.Server;

public abstract class ServerBase : BackgroundService
{
    private readonly IPeersStore _peersStore;
    private readonly IMessageQueue<IClientMessage> _messageQueue;
    private TaskCompletionSource<X509Certificate2> CertificateLoaded { get; }

    protected ServerBase(IOptions<ServerOptions> serverOptions,
        ILogger logger,
        IPeersStore peersStore,
        IMessageQueue<IClientMessage> messageQueue)
    {
        _peersStore = peersStore;
        _messageQueue = messageQueue;
        Logger = logger;
        Options = serverOptions.Value;
        CertificateLoaded = new TaskCompletionSource<X509Certificate2>();
    }

    protected ILogger Logger { get; }
    protected ServerOptions Options { get; }

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
            _ = Task.Run(async () => await ListenConsoleMessages(stoppingToken), stoppingToken);
            EnsureProtocolSupport();
            var serverCertificate = await CertificateLoaded.Task;
            var options = BootstrapServer(serverCertificate);
            await RunServerInternal(options, stoppingToken);
        }
        catch (NotSupportedException ex)
        {
            Logger.LogError(ex, "msquic is not available.");
        }
    }

    private async Task ListenConsoleMessages(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in _messageQueue.DequeueAllAsync(stoppingToken))
            {
                if (message is Unlocked unlockCommand)
                {
                    var x509 = X509CertificateLoader.LoadCertificate(unlockCommand.Certificate);
                    CertificateLoaded.SetResult(x509);
                }
            }
        }
        catch (OperationCanceledException e)
        {
            CertificateLoaded.SetCanceled(stoppingToken);
        }
        catch (Exception e)
        {
            CertificateLoaded.SetException(e);
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

    private ValueTask<QuicServerConnectionOptions> GetConnectionOptionsAsync(
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

                return _peersStore.Contains(remoteCertificate) && 
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
}