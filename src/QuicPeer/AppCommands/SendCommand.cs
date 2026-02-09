using System.Net.Quic;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common.Exceptions;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class SendCommand : AppCommand<IPeerClient>
{
    public SendCommand(ILogger<SendCommand> logger, IConsoleAccessor consoleAccessor) 
        : base(logger, consoleAccessor)
    {
    }

    public override string CommandName => "Send";

    public override async ValueTask<CommandResult> Execute(IPeerClient peerClient, CancellationToken cancellationToken)
    {
        var messagePrompt = ConsoleAccessor.TextPrompt<string>("Enter the [green]message[/] to send:");
        var message = await Console.PromptAsync(messagePrompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("Message cannot be empty.");
            return CommandResult.Fail;
        }

        try
        {
            await peerClient.SendAsync(message);
            Console.MarkupLine("[green4]Message sent.[/]");
            await Console.PromptAsync(new TextPrompt<string>("Continue").AllowEmpty(), cancellationToken);
            Console.Clear();
        }
        catch (QuicException e) when (e.IsConnectionError())
        {
            Logger.LogError(e, "Connection error.");
            Console.MarkupLine("[red]Peer disconnected.[/]");
            await ConsoleAccessor.ConfirmationPrompt().ShowAsync(Console, cancellationToken);
            Console.Clear();
            await peerClient.DisconnectAsync();
            return CommandResult.Error;
        }
        catch (Exception ex)
        {
            Console.MarkupLine("[red]Failed to send message[/]");

            Logger.LogError(ex, "Couldn't send message.");
            return CommandResult.Fail;
        }
        
        return CommandResult.Success;
    }
}
