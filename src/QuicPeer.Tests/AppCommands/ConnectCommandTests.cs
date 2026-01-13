using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuicPeer.AppCommands;
using QuicPeer.Client;
using Spectre.Console;
using static QuicPeer.Tests.AppCommands.AppCommandsMock;

namespace QuicPeer.Tests.AppCommands;

public sealed class ConnectCommandTests : IDisposable
{
    private readonly CancellationTokenSource _cts = new (100);
    private readonly PeerConnector _peerConnector = Substitute.For<PeerConnector>(Substitute.For<IPeerClientFactory>());
    private readonly ILogger<ConnectCommand> _logger = Substitute.For<ILogger<ConnectCommand>>();
    private readonly IConsoleAccessor _consoleAccessor = Substitute.For<IConsoleAccessor>();

    [Fact]
    public async Task should_exit_without_error_when_connection_fails()
    {
        var connectionException = new Exception("Connection error");
        var peerConnector = Substitute.For<PeerConnector>(Substitute.For<IPeerClientFactory>());
        peerConnector.Connect(Arg.Any<string>(), 
            Arg.Any<CancellationToken>()).ThrowsForAnyArgs(connectionException);
        var command = new ConnectCommand(_logger, _consoleAccessor,
            peerConnector, ConnectCommandMock.SendCommand, ConnectCommandMock.SendFileCommand);
        
        await command.Execute(_cts.Token);

        await peerConnector.Received(1).Connect(Arg.Any<string>(), Arg.Any<CancellationToken>());

        _logger.Received(1).Log(LogLevel.Error, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), 
            connectionException, Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task should_connect_to_endpoint_given_by_user()
    {
        const string expectedEndpoint = "remote.point:501";
        var peerConnector = Substitute.For<PeerConnector>(Substitute.For<IPeerClientFactory>());
        var command = new ConnectCommand(_logger, _consoleAccessor,
            peerConnector, ConnectCommandMock.SendCommand, ConnectCommandMock.SendFileCommand);

        var textPrompt = Substitute.For<IPrompt<string>>();
        textPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(expectedEndpoint);
        _consoleAccessor.TextPrompt<string>(Arg.Any<string>()).Returns(textPrompt);

        await command.Execute(_cts.Token);

        await peerConnector.Received(1)
            .Connect(Arg.Is<string>(endpoint => endpoint == expectedEndpoint), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_show_sub_menu_when_connection_success()
    {
        var subMenu = Substitute.For<IPrompt<string>>();
        subMenu.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("");

        _consoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>()).ReturnsForAnyArgs(subMenu);

        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Substitute.For<IPeerClient>());

        var command = new ConnectCommand(_logger, _consoleAccessor,
                _peerConnector, 
                ConnectCommandMock.SendCommand, 
                ConnectCommandMock.SendFileCommand);
        
        await command.Execute(_cts.Token);

        await subMenu.Received()
            .ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_execute_send_subcommand()
    {

        var subMenu = Substitute.For<IPrompt<string>>();
        subMenu.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Send");

        _consoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>()).ReturnsForAnyArgs(subMenu);

        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Substitute.For<IPeerClient>());

        var command = new ConnectCommand(_logger, _consoleAccessor,
                _peerConnector,
                ConnectCommandMock.SendCommand,
                ConnectCommandMock.SendFileCommand);
        
        await command.Execute(_cts.Token);

        await ConnectCommandMock.SendCommand.Received().Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_execute_send_file_subcommand()
    {
        var subMenu = Substitute.For<IPrompt<string>>();
        subMenu.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs("Send file");
        _consoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>())
            .ReturnsForAnyArgs(subMenu);
        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(Substitute.For<IPeerClient>());

        var command = new ConnectCommand(_logger, _consoleAccessor,
                _peerConnector,
                ConnectCommandMock.SendCommand,
                ConnectCommandMock.SendFileCommand);
        
        await command.Execute(_cts.Token);

        await ConnectCommandMock.SendFileCommand
            .Received().Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());
        await ConnectCommandMock.SendCommand
            .Received(0).Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());
    }
    
    [Fact]
    public async Task should_exit_command_when_disconnect_option_selected()
    {
        var subMenu = Substitute.For<IPrompt<string>>();
        subMenu.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs("Disconnect");
        _consoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>())
            .ReturnsForAnyArgs(subMenu);
        _consoleAccessor.ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(true);
        _consoleAccessor.SpinnerAsync(Arg.Any<string>(), Arg.Any<Task<IPeerClient?>>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(c => c[1] as Task<IPeerClient?>);
        
        var peerClient = Substitute.For<IPeerClient>();
        peerClient.DisconnectAsync().Returns(Task.CompletedTask);
        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(peerClient);

        var command = new ConnectCommand(_logger, _consoleAccessor,
                _peerConnector,
                ConnectCommandMock.SendCommand,
                ConnectCommandMock.SendFileCommand);
        
        await command.Execute(_cts.Token);

        await ConnectCommandMock.SendFileCommand
            .Received(0).Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());
        await ConnectCommandMock.SendCommand
            .Received(0).Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());

        await subMenu
            .Received(1).ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>());
       
        await peerClient.Received(1).DisconnectAsync();
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
