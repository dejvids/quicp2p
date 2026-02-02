using System.Net.Quic;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Server;

public sealed class PeerServer(
    IOptions<ServerOptions> configuration,
    ILogger<PeerServer> logger,
    IServiceScopeFactory scopeFactory,
    IHostApplicationLifetime appLifetime)
    : ServerBase(configuration, logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var cts =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, appLifetime.ApplicationStopping);
        var restarts = 0;
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                await RunServerAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                Logger.LogInformation("Server stopped");
                return;
            }
            catch (Exception ex) when(++restarts < 5)
            {
                Logger.LogCritical(ex, "Server Error. Restarting server.");
                await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
            }
        }
    }

    protected override async Task RunServerInternal(QuicListenerOptions options, CancellationToken stoppingToken)
    {
        var listener = await QuicListener.ListenAsync(options, stoppingToken);
        await ListenAsync(listener, stoppingToken);
    }

    private async Task ListenAsync(QuicListener listener, CancellationToken ct)
    {
        Logger.LogInformation("Listening on {Endpoint}", listener.LocalEndPoint);
        try
        {
            while (!ct.IsCancellationRequested)
            {
                var newConnection = await listener.AcceptConnectionAsync(ct);

                _ = OnPeerConnected(newConnection, ct);
            }
        }
        finally
        {
            await listener.DisposeAsync();
        }
    }

    private Task OnPeerConnected(QuicConnection newConnection, CancellationToken ct)
    {
        return Task.Run(async () =>
        {
            var context = ConnectionContext.Create(newConnection);
            Logger.LogInformation("New connection from {RemoteEndpoint}", context.RemoteEndPoint);
            var scope = scopeFactory.CreateAsyncScope();
            var connectionManager = scope.ServiceProvider.GetRequiredService<ConnectionManager>();
            try
            {
                await connectionManager.Process(context, ct);
            }
            catch (QuicException e) when (e.QuicError == QuicError.ConnectionAborted)
            {
                Logger.LogInformation(e, "Connection with {Endpoint} has been closed by the client",
                    context.RemoteEndPoint);
            }
            catch (OperationCanceledException e)
            {
                Logger.LogInformation(e, "Connection closed by the server.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "An error occurred while processing connection");
            }
            finally
            {
                await context.DisposeAsync();
                await scope.DisposeAsync();
            }
        }, ct);
    }
}