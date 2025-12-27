using System.Runtime.Versioning;
using QuicPeer;
using QuicPeer.Client;
using QuicPeer.Server;

[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]
[assembly: SupportedOSPlatform("macos")]

const int defaultPort = 501;

var serverPort = args.Length > 0 && int.TryParse(args[0], out var customPort) ? customPort : defaultPort;
var server = new PeerServer(serverPort);

await server.RunServerAsync();

while (true)
{
    var command = Console.ReadLine();
    if (command is null)
    {
        continue;
    }

    if (command.Equals("/exit", StringComparison.OrdinalIgnoreCase))
    {
        await server.StopAsync();
        break;
    }

    if (command.StartsWith("/connect ", StringComparison.OrdinalIgnoreCase))
    {
        var remoteEndpointArg = command[9..];
        if (!EndpointParser.TryParse(remoteEndpointArg, defaultPort, out var remoteEndpoint))
        {
            Console.WriteLine("Invalid endpoint format. Use IP:Port or Hostname:Port");
            continue;
        }

        PeerClient client = new (remoteEndpoint!);
        await client.RunClientAsync();
        while (true)
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
                        Console.WriteLine($"Failed to send message: {ex.Message}");
                    }
                }
            }
        }

    }
}
Console.ReadLine();