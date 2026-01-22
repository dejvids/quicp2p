using System.Net.Quic;
using System.Text;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Server.Commands;

namespace QuicPeer.Server;

public class ConnectionManager
{
    private readonly IFilesReceiver _filesReceiver;
    private readonly IMessageQueue<IServerCommand> _messageQueue;

    public ConnectionManager(IFilesReceiver filesReceiver, IMessageQueue<IServerCommand> messageQueue)
    {
        _filesReceiver = filesReceiver;
        _messageQueue = messageQueue;
    }

    public async Task Process(ConnectionContext context, CancellationToken ct)
    {
        await foreach (var stream in context.AcceptIncomingStreams(ct))
        {
            _ = HandleStream(stream, context, ct);
        }
    }

    private Task HandleStream(QuicStream stream, ConnectionContext context, CancellationToken ct) =>
        Task.Run(async () =>
        {
            try
            {
                if (stream.Type is QuicStreamType.Bidirectional)
                {
                    await OnTextStreamOpened(stream, context.RemoteEndpoint, context.OnFileMetadataReceived, ct);
                }
                else
                {
                    await OnFileStreamOpened(stream, context.GetFileMetadata(stream.Id), ct);
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
        await _messageQueue.EnqueueAsync(new MessageCommand(remoteEndpoint,
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