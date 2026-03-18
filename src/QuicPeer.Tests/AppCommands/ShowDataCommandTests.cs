using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Common.Messaging.ServerQueue;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace QuicPeer.Tests.AppCommands;

public class ShowDataCommandTests : AppCommandTestsBase
{
    private readonly ILogger<ShowDataCommand> _logger = Substitute.For<ILogger<ShowDataCommand>>();
    
    [Fact]
    public async Task should_show_empty_placeholder_for_empty_message()
    {
        var command = new ShowDataCommand(_logger, ConsoleAccessor);
        
        await command.Execute(Enumerable.Empty<TextReceived>(), CancellationToken.None);
        
        ConsoleAccessor.Console.Received(1).Write(Arg.Any<Text>());
    }
    
    [Fact]
    public async Task should_write_each_message_in_3_lines()
    {
        const int count = 3;
        var messages = Enumerable.Range(1, count)
            .Select(i => new TextReceived("Sender", $"Message {i}", new TimeOnly()));
        
        var command = new ShowDataCommand(_logger, ConsoleAccessor);
        
        await command.Execute(messages, CancellationToken.None);
        
        ConsoleAccessor.Console.Received(count * 3).Write(Arg.Any<IRenderable>());
    }
    
}