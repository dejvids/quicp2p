using System.Collections.Concurrent;
using System.Net.Quic;
using System.Runtime.CompilerServices;
using QuicPeer.Common.Dto;

namespace QuicPeer.Server;

public class ConnectionContext : IAsyncDisposable
{
    private readonly QuicConnection _connection;
    private readonly ConcurrentDictionary<long, FileMetadata> _files = new();

    public string RemoteEndPoint { get; }

    private ConnectionContext(QuicConnection connection)
    {
        _connection = connection;
        RemoteEndPoint = connection.RemoteEndPoint.ToString();
    }

    public static ConnectionContext Create(QuicConnection connection)
    {
        ConnectionContext context = new(connection);

        return context;
    }

    public void OnFileMetadataReceived(FileMetadata fileMetadata)
    {
        _files.AddOrUpdate(fileMetadata.DataStreamId, fileMetadata, (_, _) => fileMetadata);
    }

    public FileMetadata? GetFileMetadata(long streamId) =>
        _files.GetValueOrDefault(streamId);

    public async IAsyncEnumerable<QuicStream> AcceptIncomingStreams([EnumeratorCancellation] CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return await _connection.AcceptInboundStreamAsync(ct);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}