using System.Security.Authentication;
using System.Text;
using QuicPeer.Client.Abstraction;
using QuicPeer.Client.Exceptions;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class ConnectCommand : AppCommand
{
    private const string DisconnectCommand = "Disconnect";
    private readonly IPeerConnector _peerConnector;
    private readonly AppCommand<IPeerClient>[] _subCommands;

    public const string ConnectMenu = "connect-menu";
    public override string CommandName => "Connect";

    private readonly List<string> _subMenuOptions;

    public ConnectCommand(ILogger<ConnectCommand> logger,
        IConsoleAccessor consoleAccessor,
        IPeerConnector peerConnector,
        [FromKeyedServices(ConnectMenu)]IEnumerable<AppCommand> subCommands) : base(logger, consoleAccessor)
    {
        _peerConnector = peerConnector;
        _subCommands =  subCommands.OfType<AppCommand<IPeerClient>>().ToArray();

        _subMenuOptions = _subCommands.Select(c => c.CommandName).ToList();
        _subMenuOptions.Add(DisconnectCommand);
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
        finally
        {
            await peerClient.DisposeAsync();
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
                if (!await ConsoleAccessor.ConfirmAsync("Are you sure?", true, cancellationToken))
                {
                    Console.Clear();
                    continue;
                }

                await peerClient.DisconnectAsync();
                Console.Clear();
                break;
            }
            
            var subCommand = _subCommands.FirstOrDefault(c => c.CommandName == clientCommand);
            if (subCommand is null)
            {
                Console.Clear();
                continue;
            }
            
            result = await subCommand.Execute(peerClient, cancellationToken);
        }
    }

    private async Task<IPeerClient?> SetupConnection(CancellationToken cancellationToken)
    {
        while (true)
        {
            var textPrompt =
                ConsoleAccessor.TextPrompt<string>("Enter the [green]endpoint[/] (IP:Port or Hostname:Port):");
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
        catch (EndpointParsingException ex)
        {
            Console.MarkupLine("[red]Invalid endpoint format.[/] Please use IP:Port or Hostname:Port.");
            Logger.LogError(ex, "Could not parse endpoint.");
        }
        catch (AuthenticationException e)
        {
            Console.MarkupLine("[red]Authentication failed.[/]");
            Logger.LogError(e, "Authentication failed.");
        }
        catch (Exception ex)
        {
            Console.MarkupLine("[red]Couldn't connect to[/] [yellow]{0}[/]", endpoint);
            Logger.LogError(ex, "Connection failed");
        }

        return null;
    }
}