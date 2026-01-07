using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Options;
using QuicPeer.Options;
using QuicPeer.Server.Commands;

namespace QuicPeer.Server;

public sealed class PeerServer(IOptions<ServerOptions> configuration, ILogger<PeerServer> logger, IMessageQueue<IServerCommand> messageQueue)
    : ServerBase(configuration, logger), IAsyncDisposable
{
    private QuicListener? _listener;
    private Task? _newConnectionsHandler;
    private CancellationTokenSource _cancellationTokenSource;
    private readonly IMessageQueue<IServerCommand> _commandChannel = messageQueue;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        await RunServerAsync();

        var tcs = new TaskCompletionSource();
        using var _ = stoppingToken.Register(() => tcs.SetResult());
        await tcs.Task;
    }

    protected override async Task RunServerInternal(QuicListenerOptions options)
    {
        _listener = await QuicListener.ListenAsync(options);
        _newConnectionsHandler = Task.Factory.StartNew(async () => await HandleNewConnections(_listener, _cancellationTokenSource.Token),
            TaskCreationOptions.LongRunning);

        Logger.LogInformation("Listening on {Endpoint}", _listener.LocalEndPoint);
    }

    protected override async ValueTask<QuicServerConnectionOptions> GetConnectionOptionsAsync(X509Certificate2 serverCertificate)
    {
        var baseOptions = await base.GetConnectionOptionsAsync(serverCertificate);

        baseOptions.ServerAuthenticationOptions.ClientCertificateRequired = true;
        baseOptions.ServerAuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
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

        return baseOptions;
    }

    private async Task HandleNewConnections(QuicListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var newConnection = await listener.AcceptConnectionAsync(ct);

            Logger.LogInformation("New connection from {RemoteEndpoint}", newConnection.RemoteEndPoint);

            _ = Task.Run(async () =>
            {
                var buffer = new byte[1000];
                while (!ct.IsCancellationRequested)
                {
                    using var stream = await newConnection.AcceptInboundStreamAsync(ct);

                    var readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var payload = buffer.AsSpan(0, readBytes);
                    var message = Encoding.UTF8.GetString(payload);

                    await _commandChannel.EnqueueAsync(new MessageCommand(newConnection.RemoteEndPoint.ToString(), message, TimeOnly.FromDateTime(DateTime.Now)));
                }
            });
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation($"Stopping listener on port {501}");

        _cancellationTokenSource?.CancelAsync();

        if (_newConnectionsHandler is not null)
        {
            try
            {
                await _newConnectionsHandler;
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Accepting new connections cancelled.");
            }
        }

        await base.StopAsync(cancellationToken);

        Logger.LogWarning("Server stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (_listener is null)
        {
            return;
        }

        await _listener.DisposeAsync();
    }
}