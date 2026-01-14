using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Client;
using Spectre.Console;

namespace QuicPeer.Tests.AppCommands;

public class SendCommandTests : AppCommandTestsBase
{
    private readonly ILogger<SendCommand> _logger = Substitute.For<ILogger<SendCommand>>();

    [Fact]
    public async Task should_send_message_given_by_user()
    {
        const string message = "Hello world!";
        var peerClient = Substitute.For<IPeerClient>();

        var messagePrompt =  Substitute.For<IPrompt<string>>();
        messagePrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(message));
        ConsoleAccessor.TextPrompt<string>(Arg.Any<string>()).ReturnsForAnyArgs(messagePrompt);

        var sendCommand = new SendCommand(_logger, ConsoleAccessor);
        
        await sendCommand.Execute(peerClient, CancellationToken.None);
        
        await peerClient.Received().SendAsync(Arg.Is(message));
    }

    [Fact]
    public async Task should_not_send_if_message_is_empty()
    {
        var peerClient = Substitute.For<IPeerClient>();
        
        var messagePrompt =  Substitute.For<IPrompt<string>>();
        messagePrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(string.Empty));
        ConsoleAccessor.TextPrompt<string>(Arg.Any<string>()).ReturnsForAnyArgs(messagePrompt);

        var sendCommand = new SendCommand(_logger, ConsoleAccessor);
        
        await sendCommand.Execute(peerClient, CancellationToken.None);
        
        await peerClient.DidNotReceive().SendAsync(Arg.Any<string>());
    }
}