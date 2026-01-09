using System.Net;
using System.Net.Quic;
using System.Text;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Client;

public sealed class PeerClient(IOptions<ClientOptions> options, IPEndPoint remoteEndpoint)
    : ClientBase(options), IAsyncDisposable
{
    private CancellationTokenSource _cts = new();

    public EndPoint? RemoteEndpoint { get; private set; } 

    private QuicConnection? _connection;
    protected override async Task RunClientInternal(QuicClientConnectionOptions options, CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        options.RemoteEndPoint = remoteEndpoint;
        options.ClientAuthenticationOptions.TargetHost = remoteEndpoint.Address.ToString();

        var connection = await QuicConnection.ConnectAsync(options, ct);
        _connection = connection;
        RemoteEndpoint = connection.RemoteEndPoint;

    }

    public async Task SendAsync(string message)
    {
        if (_connection is null)
        {
            return;
        }

        var textStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, _cts.Token);

        var payload = Encoding.UTF8.GetBytes(message);
        await textStream.WriteAsync(payload);
        await textStream.FlushAsync(_cts.Token);
        textStream.CompleteWrites();
    }

    public async Task SendAsync(FileInfo file)
    {
        if (_connection is null || !File.Exists(file.FullName))
        {
            return;
        }

        var dataStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional, _cts.Token);
        using var fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read);

        try
        {
            await fileStream.CopyToAsync(dataStream, _cts.Token);

            dataStream.CompleteWrites();
        }
        finally
        {
            await dataStream.DisposeAsync();
        }
    }

    public async Task DisconnectAsync()
    {
        await _cts.CancelAsync();
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Dispose();
    }
}