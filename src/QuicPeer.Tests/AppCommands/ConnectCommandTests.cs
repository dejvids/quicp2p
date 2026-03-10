using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using QuicPeer.AppCommands;
using QuicPeer.Client.Abstraction;
using Spectre.Console;
using static QuicPeer.Tests.AppCommands.AppCommandsMock;

namespace QuicPeer.Tests.AppCommands;

public sealed class ConnectCommandTests : AppCommandTestsBase
{
    private readonly IPeerConnector _peerConnector = Substitute.For<IPeerConnector>();
    private readonly ILogger<ConnectCommand> _logger = Substitute.For<ILogger<ConnectCommand>>();

    [Fact]
    public async Task should_exit_without_error_when_connection_fails()
    {
        var connectionException = new Exception("Connection error");
        _peerConnector.Connect(Arg.Any<string>(), 
            Arg.Any<CancellationToken>()).ThrowsForAnyArgs(connectionException);
        var command = new ConnectCommand(_logger, ConsoleAccessor,
            _peerConnector, [ConnectCommandMock.SendCommand, ConnectCommandMock.SendFileCommand]);
        
        await command.Execute(CancellationToken);

        await _peerConnector.Received(1).Connect(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_connect_to_endpoint_given_by_user()
    {
        const string expectedEndpoint = "remote.point:501";
        var command = new ConnectCommand(_logger, ConsoleAccessor,
            _peerConnector, [ConnectCommandMock.SendCommand, ConnectCommandMock.SendFileCommand]);

        var textPrompt = Substitute.For<IPrompt<string>>();
        textPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(expectedEndpoint);
        ConsoleAccessor.TextPrompt<string>(Arg.Any<string>()).Returns(textPrompt);

        await command.Execute(CancellationToken);

        await _peerConnector.Received(1)
            .Connect(Arg.Is<string>(endpoint => endpoint == expectedEndpoint), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_show_sub_menu_when_connection_success()
    {
        var subMenu = Substitute.For<IPrompt<string>>();
        subMenu.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("");

        ConsoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>()).ReturnsForAnyArgs(subMenu);

        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Substitute.For<IPeerClient>());

        var command = new ConnectCommand(_logger, ConsoleAccessor,
                _peerConnector, 
                [ConnectCommandMock.SendCommand, 
                ConnectCommandMock.SendFileCommand]);
        
        await command.Execute(CancellationToken);

        await subMenu.Received()
            .ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_execute_send_subcommand()
    {

        var subMenu = Substitute.For<IPrompt<string>>();
        subMenu.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs("Send");

        ConsoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>()).ReturnsForAnyArgs(subMenu);

        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>()).ReturnsForAnyArgs(Substitute.For<IPeerClient>());

        var command = new ConnectCommand(_logger, ConsoleAccessor,
                _peerConnector,
                [ConnectCommandMock.SendCommand,
                ConnectCommandMock.SendFileCommand]);
        
        await command.Execute(CancellationToken);

        await ConnectCommandMock.SendCommand.Received().Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_execute_send_file_subcommand()
    {
        var subMenu = Substitute.For<IPrompt<string>>();
        subMenu.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs("Send file");
        ConsoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>())
            .ReturnsForAnyArgs(subMenu);
        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(Substitute.For<IPeerClient>());

        var command = new ConnectCommand(_logger, ConsoleAccessor,
                _peerConnector,
                [ConnectCommandMock.SendCommand,
                ConnectCommandMock.SendFileCommand]);
        
        await command.Execute(CancellationToken);

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
        ConsoleAccessor.SelectionPrompt(Arg.Any<IEnumerable<string>>())
            .ReturnsForAnyArgs(subMenu);
        ConsoleAccessor.ConfirmAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(true);
        ConsoleAccessor.SpinnerAsync(Arg.Any<string>(), Arg.Any<Task<IPeerClient?>>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(c => c[1] as Task<IPeerClient?>);
        
        var peerClient = Substitute.For<IPeerClient>();
        peerClient.DisconnectAsync().Returns(Task.CompletedTask);
        _peerConnector.Connect(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ReturnsForAnyArgs(peerClient);

        var command = new ConnectCommand(_logger, ConsoleAccessor,
                _peerConnector,
                [ConnectCommandMock.SendCommand,
                ConnectCommandMock.SendFileCommand]);
        
        await command.Execute(CancellationToken);

        await ConnectCommandMock.SendFileCommand
            .Received(0).Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());
        await ConnectCommandMock.SendCommand
            .Received(0).Execute(Arg.Any<IPeerClient>(), Arg.Any<CancellationToken>());

        await subMenu
            .Received(1).ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>());
       
        await peerClient.Received(1).DisconnectAsync();
    }
}
