using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Tests.AppCommands;
using Spectre.Console;

namespace QuicPeer.Tests;

public sealed class ConsoleAppTests : IDisposable
{
    private readonly IConsoleAccessor _consoleAccessor = Substitute.For<IConsoleAccessor>();
    private readonly IPrompt<string> _menuPrompt;
    private readonly CancellationTokenSource _cts = new(200);

    public ConsoleAppTests()
    {
        var console = Substitute.For<IAnsiConsole>();
        _consoleAccessor.Console.Returns(console);
        _menuPrompt = Substitute.For<IPrompt<string>>();

        _consoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>()).ReturnsForAnyArgs(_menuPrompt);
    }

    [Fact]
    public async Task should_start_app()
    {
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
           [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand],
           AppCommandsMock.UnlockCommand,
           Substitute.For<IHostApplicationLifetime>());

        await consoleApp.StartAsync(_cts.Token);
    }

    [Fact]
    public async Task should_set_console_encoding_to_utf8()
    {
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(), _consoleAccessor,
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand],
            AppCommandsMock.UnlockCommand,
            Substitute.For<IHostApplicationLifetime>());

        await consoleApp.StartAsync(_cts.Token);
        Assert.Equal(Encoding.UTF8, Console.OutputEncoding);
        Assert.Equal(Encoding.UTF8, Console.InputEncoding);
    }

    [Fact]
    public async Task should_exit_app_if_got_exit_command()
    {
        _menuPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Exit");
        _consoleAccessor.ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(true);
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand],
            AppCommandsMock.UnlockCommand,
            Substitute.For<IHostApplicationLifetime>());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await consoleApp.StartAsync(cts.Token);
        await consoleApp.AppRunner;

        await _menuPrompt.Received(1).ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>());
        await _consoleAccessor.Received(1).ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_call_connect_command()
    {
        var connectCommand = AppCommandsMock.ConnectCommand;
        _menuPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Connect");
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            [connectCommand, AppCommandsMock.ShowDataCommand],
            AppCommandsMock.UnlockCommand,
            Substitute.For<IHostApplicationLifetime>());

        await consoleApp.StartAsync(_cts.Token);
        await consoleApp.Activated;

        await connectCommand.Received().Execute(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_call_data_command()
    {
        var showDataCommand = AppCommandsMock.ShowDataCommand;
        _menuPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Data");
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            [AppCommandsMock.ConnectCommand, showDataCommand],
            AppCommandsMock.UnlockCommand,
            Substitute.For<IHostApplicationLifetime>());

        await consoleApp.StartAsync(_cts.Token);
        await consoleApp.Activated;

        await showDataCommand.Received().Execute(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_exit_if_could_not_unlock()
    {
        var appLifetime = Substitute.For<IHostApplicationLifetime>();
        var unlockCommand = AppCommandsMock.UnlockCommand;
        unlockCommand.Execute(Arg.Any<CancellationToken>()).Returns(CommandResult.Error);
        var consoleApp = new ConsoleApp(Substitute.For<ILogger<ConsoleApp>>(),
            _consoleAccessor,
            [AppCommandsMock.ConnectCommand, AppCommandsMock.ShowDataCommand],
            unlockCommand,
            appLifetime);

        await consoleApp.StartAsync(_cts.Token);

        appLifetime.Received(1).StopApplication();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
