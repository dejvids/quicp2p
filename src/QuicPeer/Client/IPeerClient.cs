using System.Net;

namespace QuicPeer.Client;

public interface IPeerClient : IAsyncDisposable
{
    EndPoint? RemoteEndpoint { get; }

    Task SendAsync(string message);
    Task SendFileAsync(FileInfo file);
    Task DisconnectAsync();
}
