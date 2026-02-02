using System.Text;
using QuicPeer.Client;
using QuicPeer.Client.Exceptions;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class ConnectCommand : AppCommand
{
    private const string DisconnectCommand = "Disconnect";
    private readonly PeerConnector _peerConnector;

    public override string CommandName => "Connect";
    private SendCommand SendCommand { get; }
    private SendFileCommand SendFileCommand { get; }

    private readonly List<string> _subMenuOptions;

    public ConnectCommand(ILogger<ConnectCommand> logger,
                          IConsoleAccessor consoleAccessor,
                          PeerConnector peerConnector,
                          SendCommand sendCommand,
                          SendFileCommand sendFileCommand) : base(logger, consoleAccessor)
    {
        _peerConnector = peerConnector;
        SendCommand = sendCommand;
        SendFileCommand = sendFileCommand;

        _subMenuOptions = [SendCommand.CommandName, SendFileCommand.CommandName, DisconnectCommand];
    }

    public override async ValueTask<CommandResult> Execute(CancellationToken cancellationToken)
    {
        var peerClient = await SetupConnection(cancellationToken);
        if (peerClient is null)
        {
            return CommandResult.Fail;
        }

        System.Console.InputEncoding = Encoding.UTF8;
        System.Console.OutputEncoding = Encoding.UTF8;
        AnsiConsole.MarkupLine("[green]:check_mark:[/] Connected to {0} ", peerClient.RemoteEndpoint!);
        try
        {
            await KeepConnection(peerClient, cancellationToken);
        }
        catch (Exception)
        {
            return CommandResult.Fail;
        }
        
        return CommandResult.Success;
    }

    private async Task KeepConnection(IPeerClient peerClient, CancellationToken cancellationToken)
    {
        var subMenu = ConsoleAccessor.SelectionPrompt(_subMenuOptions);
        CommandResult? result = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (result?.Exit is true)
            {
                return;
            }
            
            var clientCommand = await Console.PromptAsync(subMenu, cancellationToken);

            if (clientCommand.Equals(DisconnectCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (! await ConsoleAccessor.ConfirmAsync("Are you sure?", true, cancellationToken))
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
                result = await SendCommand.Execute(peerClient, cancellationToken);
                continue;
            }

            if (clientCommand.Equals(SendFileCommand.CommandName))
            {
                result = await SendFileCommand.Execute(peerClient, cancellationToken);
            }

        }
    }

    private async Task<IPeerClient?> SetupConnection(CancellationToken cancellationToken)
    {
        while (true)
        {
            var textPrompt = ConsoleAccessor.TextPrompt<string>("Enter the [green]endpoint[/] (IP:Port or Hostname:Port):");
            var endpoint = await Console.PromptAsync(textPrompt, cancellationToken);
            var peerClient = await ConsoleAccessor.SpinnerAsync("Connecting...", 
                Connect(endpoint, cancellationToken), cancellationToken);

            if (peerClient is null)
            {
                if (await ConsoleAccessor.ConfirmAsync("Retry?", true, cancellationToken))
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

    private async Task<IPeerClient?> Connect(string endpoint, CancellationToken cancellationToken)
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
