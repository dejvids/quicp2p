using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ServerQueue;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QuicPeer.Tests.AppCommands;

public class ShowDataCommandTests : AppCommandTestsBase
{
    private readonly ILogger<ShowDataCommand> _logger = Substitute.For<ILogger<ShowDataCommand>>();
    private readonly IMessageQueue<IServerMessage> _messageQueue = Substitute.For<IMessageQueue<IServerMessage>>();

    [Fact]
    public async Task should_show_empty_placeholder_for_empty_message()
    {
        _messageQueue.TryDequeue(out Arg.Any<IServerMessage?>()!).Returns(false);

        var command = new ShowDataCommand(_logger, ConsoleAccessor, _messageQueue);

        await command.Execute(CancellationToken.None);

        ConsoleAccessor.Console.Received(1).Write(Arg.Any<Text>());
    }

    [Fact]
    public async Task should_write_each_message_in_3_lines()
    {
        const int count = 3;
        var messages = Enumerable.Range(1, count)
            .Select(i => (IServerMessage)new TextReceived("Sender", $"Message {i}", new TimeOnly()))
            .ToArray();

        var index = 0;
        _messageQueue.TryDequeue(out Arg.Any<IServerMessage?>()!)
            .Returns(call =>
            {
                if (index >= messages.Length)
                {
                    call[0] = null;
                    return false;
                }
                call[0] = messages[index++];
                return true;
            });

        var command = new ShowDataCommand(_logger, ConsoleAccessor, _messageQueue);

        await command.Execute(CancellationToken.None);

        ConsoleAccessor.Console.Received(count * 3).Write(Arg.Any<IRenderable>());
    }
}
