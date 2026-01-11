using QuicPeer.Client;
using QuicPeer.Client.Exceptions;
using Spectre.Console;
using Spectre.Console.Extensions;

namespace QuicPeer.AppCommands;

public class ConnectCommand : AppCommand
{
    private const string DisconnectCommand = "Disconnect";
    private readonly PeerConnector _peerConnector;

    public override string CommandName { get; } = "Connect";
    protected SendCommand SendCommand { get; }
    protected SendFileCommand SendFileCommand { get; }

    public ConnectCommand(ILogger<ConnectCommand> logger,
                          IAnsiConsole console,
                          PeerConnector peerConnector,
                          SendCommand sendCommand,
                          SendFileCommand sendFileCommand) : base(logger, console)
    {
        _peerConnector = peerConnector;
        SendCommand = sendCommand;
        SendFileCommand = sendFileCommand;
    }

    public override async ValueTask Execute(CancellationToken cancellationToken)
    {
        IPeerClient? peerClient = await SetupConnection(cancellationToken);

        if (peerClient is null)
        {
            return;
        }

        Console.MarkupLine("[green]:check_mark: Connected to [/] {0} ", peerClient.RemoteEndpoint!);
        await KeepConnection(peerClient, cancellationToken);
    }

    private async Task KeepConnection(IPeerClient? peerClient, CancellationToken cancellationToken)
    {
        while (peerClient is not null && !cancellationToken.IsCancellationRequested)
        {
            var clientCommand = await Console.PromptAsync(new SelectionPrompt<string>()
                .AddChoices(SendCommand.CommandName, SendFileCommand.CommandName, DisconnectCommand), cancellationToken);

            if (clientCommand.Equals(DisconnectCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (!Console.Confirm("Are you sure?"))
                {
                    Console.Clear();
                    continue;
                }

                await peerClient.DisconnectAsync();
                Console.Clear();
                break;
            }

            if (clientCommand.Equals(SendCommand.CommandName))
            {
                await SendCommand.Execute(peerClient, cancellationToken);
                continue;
            }

            if (clientCommand.Equals(SendFileCommand.CommandName))
            {
                await SendFileCommand.Execute(peerClient, cancellationToken);
                continue;
            }

        }
    }

    private async Task<PeerClient?> SetupConnection(CancellationToken cancellationToken)
    {
        while (true)
        {
            var endpoint = await Console.AskAsync<string>("Enter the [green]endpoint[/] (IP:Port or Hostname:Port):", cancellationToken);

            var peerClient = await Console.Status()
                .Spinner(Spinner.Known.Line)
                .StartAsync("Conecting...", async ctx => await Connect(endpoint, cancellationToken));

            if (peerClient is null)
            {
                if (await Console.ConfirmAsync("Retry?", true, cancellationToken))
                {
                    continue;
                }

                Console.Clear();
                return null;
            }

            Console.Clear();
            return peerClient;
        }
    }

    private async Task<PeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            return await _peerConnector.Connect(endpoint, cancellationToken);

        }
        catch (EndpointParsingException)
        {
            Console.MarkupLine("[red]Invalid endpoint format.[/] Please use IP:Port or Hostname:Port.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Connection failed");

            Console.MarkupLine("[red]Couldn't connect to[/] [yellow]{0}[/]", endpoint);
            return null;
        }
    }
}
