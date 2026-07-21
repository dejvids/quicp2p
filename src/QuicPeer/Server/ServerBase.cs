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
    private readonly TaskCompletionSource _activatedTcs = new();
    private readonly TaskCompletionSource<X509Certificate2> _certificateLoadedCts = new();

    public Task Activated => _activatedTcs.Task;

    protected ServerBase(IOptions<ServerOptions> serverOptions,
        ILogger logger,
        IPeersStore peersStore,
        IMessageQueue<IClientMessage> messageQueue)
    {
        _peersStore = peersStore;
        _messageQueue = messageQueue;
        Logger = logger;
        Options = serverOptions.Value;
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
            var serverCertificate = await LoadCertificate(stoppingToken);
            var options = BootstrapServer(serverCertificate);
            await RunServerInternal(options, stoppingToken);
        }
        catch (NotSupportedException ex)
        {
            Logger.LogError(ex, "msquic is not available.");
        }
    }
    protected virtual void EnsureProtocolSupport()
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

    protected virtual async Task<X509Certificate2> LoadCertificate(CancellationToken stoppingToken) => 
        await _certificateLoadedCts.Task.WaitAsync(stoppingToken);

    private async Task ListenConsoleMessages(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in _messageQueue.DequeueAllAsync(stoppingToken))
            {
                if (message is Unlocked unlockCommand)
                {
                    var x509 = X509CertificateLoader.LoadPkcs12(unlockCommand.Certificate, unlockCommand.Password);
                    _certificateLoadedCts.SetResult(x509);
                }
            }

            _activatedTcs.SetResult();
        }
        catch (OperationCanceledException)
        {
            _certificateLoadedCts.SetCanceled(stoppingToken);
        }
        catch (Exception e)
        {
            _certificateLoadedCts.SetException(e);
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
        connectionOptions.ServerAuthenticationOptions.ClientCertificateRequired = Options.RequireClientCertificate;
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

                var remoteCertificate = new Certificate(new X509Certificate2(certificate));

                return _peersStore.Contains(remoteCertificate) && 
                       !remoteCertificate.IsExpired(TimeProvider.System) &&
                       (!Options.ValidateFullChain || sslPolicyErrors == SslPolicyErrors.None);
            };
    }

    protected abstract Task RunServerInternal(QuicListenerOptions options, CancellationToken stoppingToken);
}