using System.Net;
using System.Threading.Channels;

namespace QuicPeer.Client;

public class PeerConnector(IPeerClientFactory clientFactory, ILogger<PeerConnector> logger, Channel<string> commandCahnnel) : IHostedService
{
    private readonly ILogger<PeerConnector> _logger = logger;
    private readonly IPeerClientFactory _clientFactory = clientFactory;
    private readonly Channel<string> _commandChannel = commandCahnnel;
    public async Task RunAsync(CancellationToken ct)
    {
        _logger.LogInformation("PeerConnector is running.");

        CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        await foreach (var command in _commandChannel.Reader.ReadAllAsync(ct))
        {
            if (command is null)
            {
                continue;
            }

            if (command.StartsWith("/connect ", StringComparison.OrdinalIgnoreCase))
            {
                var remoteEndpointArg = command[9..];
                IPEndPoint remoteEndpoint;
                try
                {
                    remoteEndpoint = EndpointParser.Parse(remoteEndpointArg, 501);
                }
                catch(ArgumentException ex)
                {
                    _logger.LogError(ex, "Failed to parse remote endpoint {Endpoint}", remoteEndpointArg);
                    Console.WriteLine("Invalid endpoint format. Please use IP:Port or Hostname:Port.");
                    continue;
                }

                PeerClient client = _clientFactory.CreatePeerClient(remoteEndpoint);
                
                try
                {
                    await client.RunClientAsync(cts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to {RemoteEndpoint}", remoteEndpoint);
                    continue;
                }

                while (!ct.IsCancellationRequested)
                {
                    var clientCommand = Console.ReadLine();
                    if (clientCommand is null)
                    {
                        continue;
                    }

                    if (clientCommand.Equals("/disconnect", StringComparison.OrdinalIgnoreCase))
                    {
                        await client!.DisconnectAsync();
                        break;
                    }

                    if (clientCommand.StartsWith("/send ", StringComparison.OrdinalIgnoreCase))
                    {
                        var message = clientCommand[6..];
                        if (string.IsNullOrWhiteSpace(message))
                        {
                            Console.WriteLine("Message cannot be empty.");
                            continue;
                        }
                        if (client is not null)
                        {
                            try
                            {
                                await client.SendAsync(message);
                                Console.WriteLine("Message sent.");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to send message");
                            }
                        }
                    }
                }

            }
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PeerConnector is stopping.");
        return Task.CompletedTask;
    }
}
