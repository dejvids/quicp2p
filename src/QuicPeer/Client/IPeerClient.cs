using System.IO.Abstractions;
using System.Net;

namespace QuicPeer.Client;

public interface IPeerClient : IAsyncDisposable
{
    EndPoint? RemoteEndpoint { get; }

    Task SendAsync(string message);
    Task SendFileAsync(IFileInfo file);
    Task DisconnectAsync();
}
