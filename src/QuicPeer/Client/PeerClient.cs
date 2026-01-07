using System.Net;
using System.Net.Quic;
using System.Text;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Client;

public sealed class PeerClient(ILogger<PeerClient> logger, IOptions<ClientOptions> options, IPEndPoint remoteEndpoint) 
    : ClientBase(logger, options), IAsyncDisposable
{
    private CancellationTokenSource _cts = new();

    private QuicConnection? _connection;
    protected override async Task RunClientInternal(QuicClientConnectionOptions options, CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        options.RemoteEndPoint = remoteEndpoint;
        options.ClientAuthenticationOptions.TargetHost = remoteEndpoint.Address.ToString();

        try
        {
            var connection = await QuicConnection.ConnectAsync(options, ct);
            _connection = connection;
            
            Logger.LogInformation("Connected to {Endpoint}", remoteEndpoint);
        }
        catch (QuicException ex)
        {
            Logger.LogError(ex,"Failed to connect to {Endpoint}", remoteEndpoint);
            throw;
        }
    }

    internal async Task SendAsync(string message)
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
        textStream.Close();
    }


    public async Task DisconnectAsync()
    {
        await _cts.CancelAsync();
        Logger.LogInformation("Peer disconnected.");
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Dispose();
    }
}