using System.Text;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.Server.Commands;
using QuicPeer.Tests.AppCommands;
using Spectre.Console;

namespace QuicPeer.Tests;

public class ConsoleAppTests
{
    private readonly IConsoleAccessor _consoleAccessor = Substitute.For<IConsoleAccessor>();
    private readonly IPrompt<string> _menuPrompt;
    private readonly CancellationTokenSource _cts = new(100);

    public ConsoleAppTests()
    {
        var console = Substitute.For<IAnsiConsole>();
        _consoleAccessor.Console.Returns(console);
        _menuPrompt = Substitute.For<IPrompt<string>>();
        _menuPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(c =>
        {

            return Task.FromResult("Exit");
        });

        _consoleAccessor.SelectionPrompt<string>(Arg.Any<IEnumerable<string>>()).ReturnsForAnyArgs(c =>
        {
            return _menuPrompt;
        });
    }
    [Fact]
    public async Task should_set_console_encoding_to_utf8()
    {
        var consoleapp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
            Substitute.For<IMessageQueue<IServerCommand>>(),
            AppCommandsMock.ConnectCommand,
            AppCommandsMock.ShowDataCommand);

        await consoleapp.StartAsync(_cts.Token);

        Assert.Equal(Encoding.UTF8, Console.OutputEncoding);
        Assert.Equal(Encoding.UTF8, Console.InputEncoding);
    }

    [Fact]
    public async Task should_show_main_menu()
    {
        var consoleapp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
           Substitute.For<IMessageQueue<IServerCommand>>(),
           AppCommandsMock.ConnectCommand,
           AppCommandsMock.ShowDataCommand);

        await consoleapp.StartAsync(_cts.Token);

        _consoleAccessor.Received(1).SelectionPrompt(Arg.Is<IEnumerable<string>>(o => o.Contains("Connect") && o.Contains("Data") && o.Contains("Exit")));
        await _menuPrompt.Received().ShowAsync(_consoleAccessor.Console, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_read_from_server_message_queue()
    {
        var serverMessageQueue = Substitute.For<IMessageQueue<IServerCommand>>();
        var consoleapp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
            serverMessageQueue,
            AppCommandsMock.ConnectCommand,
            AppCommandsMock.ShowDataCommand);

        await consoleapp.StartAsync(_cts.Token);

        serverMessageQueue.Received().DequeueAllAsync(Arg.Any<CancellationToken>());
    }
}

