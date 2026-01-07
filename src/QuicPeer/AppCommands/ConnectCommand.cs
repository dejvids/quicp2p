using QuicPeer.Client;
using Spectre.Console;
using Spectre.Console.Extensions;

namespace QuicPeer.AppCommands;

internal class ConnectCommand : AppCommand
{
    private const string DisconnectCommand = "Disconnect";
    private readonly PeerConnector _peerConnector;

    public override string CommandName { get; } = "Connect";
    protected SendCommand SendCommand { get; } = new();

    public ConnectCommand(PeerConnector peerConnector)
    {
        _peerConnector = peerConnector;

    }

    protected override async ValueTask Execute(CancellationToken cancellationToken)
    {
        var endpoint = AnsiConsole.Ask<string>("Enter the [green]endpoint[/] (IP:Port or Hostname:Port):");


        var peerClient = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Line)
            .StartAsync("Conecting...", async ctx => await _peerConnector.Connect(endpoint, cancellationToken));

        AnsiConsole.Clear();
        if (peerClient is null)
        {
            AnsiConsole.MarkupLine("[red]Couldn't connect to[/] [yellow]{0}[/]", endpoint);
            return;
        }

        while (peerClient is not null && !cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLine("[green]:check_mark: Connected to {0}[/] ", endpoint);
            var clientCommand = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .AddChoices(SendCommand.CommandName, DisconnectCommand));

            if (clientCommand.Equals(DisconnectCommand, StringComparison.OrdinalIgnoreCase))
            {
                if(!AnsiConsole.Confirm("Are you sure?"))
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
            }
        }
    }
}
