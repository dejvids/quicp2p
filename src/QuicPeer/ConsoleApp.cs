using System.Collections.Concurrent;
using System.Text;
using QuicPeer.AppCommands;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ServerQueue;
using Spectre.Console;

namespace QuicPeer;

public class ConsoleApp : IHostedService
{
    private const string ExitCommand = "Exit";
    private readonly IMessageQueue<IServerMessage> _serverMessageQueue;
    private readonly ILogger _logger;
    private readonly IAnsiConsole _console;
    private readonly IConsoleAccessor _consoleAccessor;
    private readonly Dictionary<string, AppCommand> _appCommands;
    private readonly ConcurrentQueue<TextReceived> _messages = new();
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly UnlockCommand _unlockCommand;

    public const string MainMenu = "main-menu";
    public Task AppRunner {get; private set;} = Task.CompletedTask;
    static ConsoleApp()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;
    }

    public ConsoleApp(ILogger<ConsoleApp> logger,
        IConsoleAccessor consoleAccessor,
        IMessageQueue<IServerMessage> serverMessageQueue,
        [FromKeyedServices(MainMenu)] IEnumerable<AppCommand> appCommands,
        UnlockCommand unlockCommand,
        IHostApplicationLifetime appLifetime)
    {
        _logger = logger;
        _consoleAccessor = consoleAccessor;
        _console = consoleAccessor.Console;
        _serverMessageQueue = serverMessageQueue;
        _appLifetime = appLifetime;
        _unlockCommand = unlockCommand;

        _appCommands = appCommands.ToDictionary(c => c.CommandName);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Console app started.");
        var unlockResult = await _unlockCommand.Execute(cancellationToken);
        if (!unlockResult.IsSuccess)
        {
            _appLifetime.StopApplication();
            return;
        }
        var menuOptions = new List<string>(_appCommands.Values.Select(c => c.CommandName))
        {
            ExitCommand
        };

        var mainMenu = _consoleAccessor.SelectionPrompt(menuOptions);
        _ = Task.Factory.StartNew(async () => await ReadServerCommands(cancellationToken), cancellationToken,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);

        try
        {
            AppRunner = Task.Factory.StartNew(async () => await ShowMenu(mainMenu, cancellationToken),
                TaskCreationOptions.LongRunning);
        }
        catch (Exception ex)
        {
            _console.MarkupLine("Console app has been stopped due to unexpected error.");
            _logger.LogCritical(ex, "Critical error in console app.");
        }
    }

    private async Task ShowMenu(IPrompt<string> mainMenu, CancellationToken cancellationToken)
    {
        CommandResult? result = null;
        while (!cancellationToken.IsCancellationRequested)
        {
            if (result?.Exit is true)
            {
                return;
            }

            var userCommand = await mainMenu.ShowAsync(_console, cancellationToken);

            if (userCommand.Equals(ExitCommand, StringComparison.OrdinalIgnoreCase))
            {
                if (await _consoleAccessor.ConfirmAsync("Do you want to close the app?", true, cancellationToken))
                {
                    _appLifetime.StopApplication();
                    break;
                }

                continue;
            }

            if (!_appCommands.TryGetValue(userCommand, out var appCommand))
            {
                continue;
            }

            if (appCommand is ShowDataCommand dataCommand)
            {
                await dataCommand.Execute(_messages, cancellationToken);
                continue;
            }

            result = await appCommand.Execute(cancellationToken);
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
                if (command is not TextReceived message)
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