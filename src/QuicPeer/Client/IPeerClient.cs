using System.IO.Abstractions;
using System.Net;

namespace QuicPeer.Client;

public interface IPeerClient : IAsyncDisposable
{
    EndPoint? RemoteEndpoint { get; }

    Task SendAsync(string message);
    Task<SendFileResult> SendFileAsync(IFileInfo file);
    Task DisconnectAsync();
}
