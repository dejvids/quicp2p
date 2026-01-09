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
                          PeerConnector peerConnector,
                          SendCommand sendCommand,
                          SendFileCommand sendFileCommand) : base(logger)
    {
        _peerConnector = peerConnector;
        SendCommand = sendCommand;
        SendFileCommand = sendFileCommand;
    }

    protected override async ValueTask Execute(CancellationToken cancellationToken)
    {
        PeerClient? peerClient;

        while (true)
        {
            peerClient = await SetupConnection(cancellationToken);

            if (peerClient is null)
            {
                if (await AnsiConsole.ConfirmAsync("Retry?"))
                {
                    continue;
                }

                AnsiConsole.Clear();
                return;
            }

            AnsiConsole.Clear();
            break;
        }

        AnsiConsole.MarkupLine("[green]:check_mark: Connected to [/] {0} ", peerClient.RemoteEndpoint!);
        while (peerClient is not null && !cancellationToken.IsCancellationRequested)
        {
            var clientCommand = await AnsiConsole.PromptAsync(new SelectionPrompt<string>()
                .AddChoices(SendCommand.CommandName, SendFileCommand.CommandName, DisconnectCommand));

            if (clientCommand.Equals(DisconnectCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (!AnsiConsole.Confirm("Are you sure?"))
                {
                    AnsiConsole.Clear();
                    continue;
                }

                await peerClient.DisconnectAsync();
                AnsiConsole.Clear();
                break;
            }

            if (clientCommand.Equals(SendCommand.CommandName))
            {
                await SendCommand.Start(peerClient, cancellationToken);
                continue;
            }

            if (clientCommand.Equals(SendFileCommand.CommandName))
            {
                await SendFileCommand.Start(peerClient, cancellationToken);
                continue;
            }

        }
    }

    private async Task<PeerClient?> SetupConnection(CancellationToken cancellationToken)
    {

        var endpoint = AnsiConsole.Ask<string>("Enter the [green]endpoint[/] (IP:Port or Hostname:Port):");

        return  await AnsiConsole.Status()
            .Spinner(Spinner.Known.Line)
            .StartAsync("Conecting...", async ctx => await Connect(endpoint, cancellationToken));
    }

    private async Task<PeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
    {
        try
        {
            return await _peerConnector.Connect(endpoint, cancellationToken);

        }
        catch (EndpointParsingException)
        {
            AnsiConsole.MarkupLine("[red]Invalid endpoint format.[/] Please use IP:Port or Hostname:Port.");
            return null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Connection failed");

            AnsiConsole.MarkupLine("[red]Couldn't connect to[/] [yellow]{0}[/]", endpoint);
            return null;
        }
    }
}
