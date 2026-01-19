using System.Net.Quic;
using QuicPeer.Common.Dto;

namespace QuicPeer.Server;

public interface IFilesReceiver
{
    void AcceptFile(FileMetadata metadata);
    Task ReceiveFileAsync(QuicStream stream, CancellationToken ct);
}