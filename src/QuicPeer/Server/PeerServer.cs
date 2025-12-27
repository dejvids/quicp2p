using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
namespace QuicPeer.Server;

public sealed class PeerServer(int port) : ServerBase, IAsyncDisposable
{
    private QuicListener? _listener;
    private Task? _newConnectionsHandler;
    private CancellationTokenSource? _cancellationTokenSource;

    protected override SslApplicationProtocol ApplicationProtocol { get; } = new SslApplicationProtocol("quic-peer");

    protected override async Task RunServerInternal(QuicListenerOptions options)
    {
        options.ListenEndPoint = new IPEndPoint(IPAddress.Any, port);

        _listener = await QuicListener.ListenAsync(options);
        _cancellationTokenSource = new CancellationTokenSource();
        _newConnectionsHandler = Task.Factory.StartNew(async () => await HandleNewConnections(_listener, _cancellationTokenSource.Token),
            TaskCreationOptions.LongRunning);

        Console.WriteLine($"Listening on {_listener.LocalEndPoint}");
    }

    protected override async ValueTask<QuicServerConnectionOptions> GetConnectionOptionsAsync(X509Certificate2 serverCertificate)
    {
        var baseOptions = await base.GetConnectionOptionsAsync(serverCertificate);

        baseOptions.ServerAuthenticationOptions.ClientCertificateRequired = true;
        baseOptions.ServerAuthenticationOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (certificate is not null)
                    {
                        Console.WriteLine("Client certificate received.");
                        //Accept any certificate for poc purposes.
                        return true;
                    }
                    if (sslPolicyErrors != SslPolicyErrors.None)
                    {
                        Console.WriteLine($"SSL Policy Errors: {sslPolicyErrors}");
                        return false;
                    }
                    return true;
                };

        return baseOptions;
    }

    private async Task HandleNewConnections(QuicListener listener, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var newConnection = await listener.AcceptConnectionAsync(ct);

            Console.WriteLine($"New connection from {newConnection.RemoteEndPoint}");

            _ = Task.Run(async () =>
            {
                var buffer = new byte[1000];
                while (!ct.IsCancellationRequested)
                {
                    using var stream = await newConnection.AcceptInboundStreamAsync(ct);
                    
                    var readBytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                    var payload = buffer.AsSpan(0, readBytes);
                    var message = Encoding.UTF8.GetString(payload);
                    
                    Console.WriteLine($"Received: {message}");
                }
            });
        }
    }

    public async Task StopAsync()
    {
        Console.WriteLine($"Stopping listener on port {port}");

        _cancellationTokenSource?.CancelAsync();

        if (_newConnectionsHandler is not null)
        {
            try
            {
                await _newConnectionsHandler;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Accepting new connections cancelled");
            }
        }

        Console.WriteLine("Server stopped");
    }

    public async ValueTask DisposeAsync()
    {
        if (_listener is null)
        {
            return;
        }

        await _listener.DisposeAsync();
    }
}