using System.IO.Abstractions;
using System.Net;

namespace QuicPeer.Client.Abstraction;

public interface IPeerClient : IAsyncDisposable
{
    EndPoint? RemoteEndpoint { get; }

    Task RunClientAsync();
    Task SendAsync(string message);
    Task<SendFileResult> SendFileAsync(IFileInfo file);
    Task DisconnectAsync();
}
