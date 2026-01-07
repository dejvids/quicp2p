using System.Collections.Concurrent;
using System.Text;
using QuicPeer.AppCommands;
using QuicPeer.Client;
using QuicPeer.Server.Commands;
using Spectre.Console;

namespace QuicPeer;

public class ConsoleApp : IHostedService
{
    private const string ExitCommand = "Exit";
    private readonly IMessageQueue<IServerCommand> _serverMessageQueue;
    private readonly ILogger _logger;

    private readonly Dictionary<string, AppCommand> _appCommands;
    private readonly ConcurrentQueue<MessageCommand> _messages = new();

    public ConsoleApp(ILogger<ConsoleApp> logger,
        IMessageQueue<IServerCommand> serverMessageQueue,
        PeerConnector peerConnector)
    {
        _logger = logger;
        _serverMessageQueue = serverMessageQueue;

        AppCommand[] commands = [new ConnectCommand(peerConnector), new ShowDataCommand()];

        _appCommands = commands.ToDictionary(c => c.CommandName);

        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Console app started.");

        var mainMenu = new SelectionPrompt<string>()
            .AddChoices(_appCommands.Values.Select(c => c.CommandName))
            .AddChoices(ExitCommand);

        _ = Task.Factory.StartNew(async () => await ReadServerCommands(cancellationToken), TaskCreationOptions.LongRunning);
       
        await ShowMenu(mainMenu, cancellationToken);
    }

    private async Task ShowMenu(SelectionPrompt<string> mainMenu, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var userCommand = AnsiConsole.Prompt(mainMenu);

            if (userCommand.Equals(ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (AnsiConsole.Confirm("Do you want to close the app?"))
                {
                    await StopAsync(cancellationToken);
                    break;
                }
            }

            var appCommand = _appCommands[userCommand];

            if(appCommand is ShowDataCommand dataCommand)
            {
                await dataCommand.Start(_messages, cancellationToken);
                continue;
            }

            await appCommand.Start(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("Shutting down:");
        _logger.LogInformation("App stopped.");
        return Task.CompletedTask;
    }

    private async Task ReadServerCommands(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var command in _serverMessageQueue.DequeueAllAsync(cancellationToken))
            {
                if (command is not MessageCommand message)
                {
                    _logger.LogWarning("Unsupported command type {Type}", command.GetType().Name);
                    continue;
                }

                _messages.Enqueue(message);
            }
        }
        catch(OperationCanceledException)
        {
            return;
        }
    }
}
