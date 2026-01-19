using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Options;
using QuicPeer.Server.Commands;

namespace QuicPeer.Server;

public sealed class PeerServer(
    IOptions<ServerOptions> configuration,
    ILogger<PeerServer> logger,
    IMessageQueue<IServerCommand> messageQueue,
    IFilesReceiver filesReceiver)
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
            async () => await HandleNewConnections(_listener, _cancellationTokenSource!.Token),
            TaskCreationOptions.LongRunning);

        Logger.LogInformation("Listening on {Endpoint}", _listener.LocalEndPoint);
    }

    protected override async ValueTask<QuicServerConnectionOptions> GetConnectionOptionsAsync(
        X509Certificate2 serverCertificate)
    {
        var baseOptions = await base.GetConnectionOptionsAsync(serverCertificate);

        baseOptions.ServerAuthenticationOptions.ClientCertificateRequired = true;
        baseOptions.ServerAuthenticationOptions.RemoteCertificateValidationCallback =
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

        return baseOptions;
    }

    private async Task HandleNewConnections(QuicListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var newConnection = await listener.AcceptConnectionAsync(ct);

            Logger.LogInformation("New connection from {RemoteEndpoint}", newConnection.RemoteEndPoint);

            _ = Task.Run(async () => await KeepConnection(newConnection, ct), ct);
        }
    }

    private async Task KeepConnection(QuicConnection newConnection, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var stream = await newConnection.AcceptInboundStreamAsync(ct);
            if (stream.Type is QuicStreamType.Unidirectional)
            {
                _ = Task.Run(async () => await filesReceiver.ReceiveFileAsync(stream, ct), ct);
                continue;
            }

            _ = Task.Run(async () => await HandleTextStream(stream, newConnection.RemoteEndPoint, ct), ct);
        }
    }

    private async Task HandleTextStream(QuicStream stream,
        IPEndPoint remoteEndpoint, CancellationToken ct)
    {
        var buffer = new byte[1000];
        while (!ct.IsCancellationRequested && !stream.ReadsClosed.IsCompleted)
        {
            Array.Clear(buffer, 0, buffer.Length);
            var readBytes = await stream.ReadAsync(buffer, ct);
            var payload = buffer.AsSpan(0, readBytes);
            var message = Encoding.UTF8.GetString(payload);
            await messageQueue.EnqueueAsync(new MessageCommand(remoteEndpoint.ToString(),
                message, TimeOnly.FromDateTime(DateTime.Now)));

            if (!TryParseToFileMetadata(message, out var metadata) || metadata is null)
            {
                continue;
            }
            
            await ReplaySender(stream, metadata, ct);
        }
    }

    private async Task ReplaySender(QuicStream stream, FileMetadata metadata, CancellationToken ct)
    {
        filesReceiver.AcceptFile(metadata);
        stream.WriteByte(ControlCodes.MetadataReceived);
        await stream.FlushAsync(ct);
        stream.CompleteWrites();
    }

    private static bool TryParseToFileMetadata(string message, out FileMetadata? dto)
    {
        try
        {
            dto = System.Text.Json.JsonSerializer.Deserialize<FileMetadata>(message);
            return dto is { FileSize: > 0 };
        }
        catch (Exception)
        {
            dto = null;
            return false;
        }
    }



    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Stopping listener on port.");

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
        _cancellationTokenSource?.Dispose();
    }
}