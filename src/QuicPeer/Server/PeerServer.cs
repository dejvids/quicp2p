using System.Net.Quic;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Server;

public sealed class PeerServer(
    IOptions<ServerOptions> configuration,
    ILogger<PeerServer> logger,
    ConnectionManager connectionManager)
    : ServerBase(configuration, logger), IAsyncDisposable
{
    private QuicListener? _listener;
    private Task? _newConnectionsHandler;
    private CancellationTokenSource? _cancellationTokenSource;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            await RunServerAsync();

            var tcs = new TaskCompletionSource();
            await using var _ = stoppingToken.Register(() => tcs.SetResult());
            await tcs.Task;
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex, "Server failed");
        }
    }

    protected override async Task RunServerInternal(QuicListenerOptions options)
    {
        _listener = await QuicListener.ListenAsync(options);
        _newConnectionsHandler = Task.Factory.StartNew(
            async () => await ListenAsync(_listener, _cancellationTokenSource!.Token),
            TaskCreationOptions.LongRunning);

        Logger.LogInformation("Listening on {Endpoint}", _listener.LocalEndPoint);
    }

    private async Task ListenAsync(QuicListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var newConnection = await listener.AcceptConnectionAsync(ct);

            _ = OnPeerConnected(newConnection, ct);
        }
    }

    private Task OnPeerConnected(QuicConnection newConnection, CancellationToken ct)
    {
        Logger.LogInformation("New connection from {RemoteEndpoint}", newConnection.RemoteEndPoint);
        var context = ConnectionContext.Create(newConnection);
        return Task.Run(async () => await connectionManager.Process(context, ct), ct);
    }
    
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
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

        Logger.LogWarning("Server stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (_listener is null)
        {
            return;
        }

        await _listener.DisposeAsync();
        _cancellationTokenSource?.Dispose();
    }
}