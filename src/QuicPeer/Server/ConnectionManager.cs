using System.Net.Quic;
using System.Text;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ServerQueue;

namespace QuicPeer.Server;

public class ConnectionManager
{
    private readonly IFilesReceiver _filesReceiver;
    private readonly IMessageQueue<IServerMessage> _messageQueue;

    public ConnectionManager(IFilesReceiver filesReceiver, IMessageQueue<IServerMessage> messageQueue)
    {
        _filesReceiver = filesReceiver;
        _messageQueue = messageQueue;
    }

    public async Task Process(ConnectionContext context, CancellationToken ct)
    {
        var streamHandlers = new List<Task>();
        await foreach (var stream in context.AcceptIncomingStreams(ct))
        {
            streamHandlers.Add(HandleStream(stream, context, ct));
        }
        
        await Task.WhenAll(streamHandlers);
    }

    private Task HandleStream(QuicStream stream, ConnectionContext context, CancellationToken ct) =>
        Task.Run(async () =>
        {
            try
            {
                if (stream.Type is QuicStreamType.Bidirectional)
                {
                    await OnTextStreamOpened(stream, context.RemoteEndPoint, context.OnFileMetadataReceived, ct);
                }
                else
                {
                    await OnFileStreamOpened(stream, context.ConsumeFileMetadata(stream.Id), ct);
                }
            }
            finally
            {
                await stream.DisposeAsync();
            }
        }, ct);

    private async Task OnFileStreamOpened(QuicStream stream, FileMetadata? fileMetadata, CancellationToken ct)
    {
        if (fileMetadata is null)
        {
            return;
        }
        
        await _filesReceiver.ReceiveFileAsync(stream, fileMetadata, ct);
    }

    private async Task OnTextStreamOpened(QuicStream stream,
        string remoteEndpoint, 
        Action<FileMetadata> callback, 
        CancellationToken ct)
    {
        var buffer = new byte[1000];

        Array.Clear(buffer, 0, buffer.Length);
        var readBytes = await stream.ReadAsync(buffer, ct);
        var payload = buffer.AsSpan(0, readBytes);
        var message = Encoding.UTF8.GetString(payload);
        await _messageQueue.EnqueueAsync(new TextReceived(remoteEndpoint,
            message, TimeOnly.FromDateTime(DateTime.Now)));

        if (TryParseToFileMetadata(message, out var metadata) && metadata is not null)
        {
            callback.Invoke(metadata);
            await ReplaySender(stream, ct);
        }
    }

    private static async Task ReplaySender(QuicStream stream,  CancellationToken ct)
    {
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
}