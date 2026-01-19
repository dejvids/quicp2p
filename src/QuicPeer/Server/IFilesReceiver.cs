using QuicPeer.Common.Dto;

namespace QuicPeer.Server;

public interface IFilesReceiver
{
    Task ReceiveFileAsync(Stream stream, FileMetadata metadata, CancellationToken ct);
}