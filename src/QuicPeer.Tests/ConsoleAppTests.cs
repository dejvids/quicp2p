using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ServerQueue;
using QuicPeer.Tests.AppCommands;
using Spectre.Console;

namespace QuicPeer.Tests;

public sealed class ConsoleAppTests : IDisposable
{
    private readonly IConsoleAccessor _consoleAccessor = Substitute.For<IConsoleAccessor>();
    private readonly IPrompt<string> _menuPrompt;
    private readonly CancellationTokenSource _cts = new(500);

    public ConsoleAppTests()
    {
        var console = Substitute.For<IAnsiConsole>();
        _consoleAccessor.Console.Returns(console);
        _menuPrompt = Substitute.For<IPrompt<string>>();

        _consoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>()).ReturnsForAnyArgs(_menuPrompt);
    }
    [Fact]
    public async Task should_set_console_encoding_to_utf8()
    {
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
            Substitute.For<IMessageQueue<IServerMessage>>(),
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand], 
            AppCommandsMock.UnlockCommand,
            Substitute.For<IHostApplicationLifetime>());

        _ = consoleApp.StartAsync(_cts.Token);
        await consoleApp.AppRunner;
        Assert.Equal(Encoding.UTF8, Console.OutputEncoding);
        Assert.Equal(Encoding.UTF8, Console.InputEncoding);
    }

    [Fact]
    public async Task should_show_main_menu()
    {
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
           Substitute.For<IMessageQueue<IServerMessage>>(),
           [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand], 
           AppCommandsMock.UnlockCommand, 
           Substitute.For<IHostApplicationLifetime>());

        _ = consoleApp.StartAsync(_cts.Token);
        await consoleApp.AppRunner;
        _consoleAccessor.Received(1).SelectionPrompt(
            Arg.Is<IList<string>>(o => 
                o.Contains("Connect") && o.Contains("Data") && o.Contains("Exit")));
        await _menuPrompt.Received().ShowAsync(_consoleAccessor.Console, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_read_from_server_message_queue()
    {
        var serverMessageQueue = Substitute.For<IMessageQueue<IServerMessage>>();
        var receivedMessages = 0;
        using var cts = new CancellationTokenSource(1000);
        serverMessageQueue.DequeueAllAsync(Arg.Any<CancellationToken>()).ReturnsForAnyArgs(_ =>
        {
            return AsyncEnumerable.Range(1, 2).Select(i => 
            {
                receivedMessages++;
                return new TextReceived("Test", $"{i}", TimeOnly.FromDateTime(DateTime.Now));
            });
        });

        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
            serverMessageQueue,
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand], 
            AppCommandsMock.UnlockCommand, 
            Substitute.For<IHostApplicationLifetime>());
        
        _ = consoleApp.StartAsync(cts.Token);
        await consoleApp.AppRunner;
        serverMessageQueue.Received().DequeueAllAsync(Arg.Any<CancellationToken>());

        Assert.Equal(2, receivedMessages);
    }

    [Fact]
    public async Task should_exit_app_if_got_exit_command()
    {
        _menuPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Exit");
        _consoleAccessor.ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(true);
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            Substitute.For<IMessageQueue<IServerMessage>>(),
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand], 
            AppCommandsMock.UnlockCommand, 
            Substitute.For<IHostApplicationLifetime>());

        _ = consoleApp.StartAsync(_cts.Token);
        await consoleApp.AppRunner;
        
        await _menuPrompt.Received(1).ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>());
        await _consoleAccessor.Received(1).ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_call_connect_command()
    {
        _menuPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Connect");
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            Substitute.For<IMessageQueue<IServerMessage>>(),
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand], 
            AppCommandsMock.UnlockCommand,
            Substitute.For<IHostApplicationLifetime>());

        _ = consoleApp.StartAsync(_cts.Token);
        await consoleApp.AppRunner;
        
        await AppCommandsMock.ConnectCommand.Received().Execute(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_call_data_command()
    {
        _menuPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Data");
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            Substitute.For<IMessageQueue<IServerMessage>>(),
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand], 
            AppCommandsMock.UnlockCommand,
            Substitute.For<IHostApplicationLifetime>());

        _ = consoleApp.StartAsync(_cts.Token);
        await consoleApp.AppRunner;
        
        await AppCommandsMock.ShowDataCommand.Received().Execute(Arg.Any<IEnumerable<TextReceived>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_exit_if_could_not_unlock()
    {
        var appLifetime = Substitute.For<IHostApplicationLifetime>();
        var unlockCommand = AppCommandsMock.UnlockCommand;
        unlockCommand.Execute(Arg.Any<CancellationToken>()).Returns(CommandResult.Error);
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            Substitute.For<IMessageQueue<IServerMessage>>(),
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand], 
            unlockCommand,
            appLifetime);

        _ = consoleApp.StartAsync(_cts.Token);
        await consoleApp.AppRunner;
        
        appLifetime.Received(1).StopApplication();
    }
    
    public void Dispose()
    {
        _cts.Dispose();
    }
}


