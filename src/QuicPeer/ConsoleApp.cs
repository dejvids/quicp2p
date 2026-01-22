using System.Collections.Concurrent;
using System.Text;
using QuicPeer.AppCommands;
using QuicPeer.Server.Commands;
using Spectre.Console;

namespace QuicPeer;

public class ConsoleApp : IHostedService
{
    private const string ExitCommand = "Exit";
    private readonly IMessageQueue<IServerCommand> _serverMessageQueue;
    private readonly ILogger _logger;
    private readonly IAnsiConsole _console;
    private readonly IConsoleAccessor _consoleAccessor;
    private readonly Dictionary<string, AppCommand> _appCommands;
    private readonly ConcurrentQueue<MessageCommand> _messages = new();

    static ConsoleApp()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
    }
    
    public ConsoleApp(ILogger<ConsoleApp> logger,
       IConsoleAccessor consoleAccessor,
        IMessageQueue<IServerCommand> serverMessageQueue,
        ConnectCommand connectCommand,
        ShowDataCommand showDataCommand)
    {
        _logger = logger;
        _consoleAccessor = consoleAccessor;
        _console = consoleAccessor.Console;
        _serverMessageQueue = serverMessageQueue;

        AppCommand[] commands = [connectCommand, showDataCommand];

        _appCommands = commands.ToDictionary(c => c.CommandName);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Console app started.");

        var menuOptions = new List<string>(_appCommands.Values.Select(c => c.CommandName))
        {
            ExitCommand
        };

        var mainMenu = _consoleAccessor.SelectionPrompt(menuOptions);
        _ = Task.Factory.StartNew(async () => await ReadServerCommands(cancellationToken), TaskCreationOptions.LongRunning);

        try
        {
            await ShowMenu(mainMenu, cancellationToken);
        }
        catch (Exception ex)
        {
            _console.MarkupLine("Console app has been stopped due to unexpected error.");
            _logger.LogCritical(ex, "Critical error in console app.");
        }
    }

    private async Task ShowMenu(IPrompt<string> mainMenu, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var userCommand = await mainMenu.ShowAsync(_console, CancellationToken.None);

            if (userCommand.Equals(ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (await _consoleAccessor.ConfirmAsync("Do you want to close the app?", true, cancellationToken))
                {
                    await StopAsync(cancellationToken);
                    break;
                }

                continue;
            }

            if(!_appCommands.TryGetValue(userCommand, out var appCommand))
            {
                continue;
            }

            if (appCommand is ShowDataCommand dataCommand)
            {
                await dataCommand.Execute(_messages, cancellationToken);
                continue;
            }

            await appCommand.Execute(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _console.MarkupLine("Shutting down:");
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
        catch (OperationCanceledException)
        {
        }
    }
}
