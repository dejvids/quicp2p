using Microsoft.Extensions.Logging;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Client;
using Spectre.Console;

namespace QuicPeer.Tests.AppCommands;

public class SendFileCommandTests
{
    [Fact]
    public async Task should_exit_if_file_does_not_exist()
    {
        var console = Substitute.For<IAnsiConsole>();
        var sendFileCommand = new SendFileCommand(Substitute.For<ILogger<SendFileCommand>>(), console);
        var peerClient = Substitute.For<IPeerClient>();

        _ = Record.ExceptionAsync(() => sendFileCommand.Execute(peerClient, CancellationToken.None).AsTask());

        await peerClient.Received(0).SendFileAsync(Arg.Any<FileInfo>());
    }
}
