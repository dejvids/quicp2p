using QuicPeer.Client;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class SendCommand : AppCommand<IPeerClient>
{
    public SendCommand(ILogger<SendCommand> logger, IAnsiConsole console) : base(logger, console)
    {
    }

    public override string CommandName => "Send";

    public override async ValueTask Execute(IPeerClient peerClient, CancellationToken cancellationToken)
    {
        var message = await Console.AskAsync<string>("Enter the [green]message[/] to send:", cancellationToken);
        if (string.IsNullOrWhiteSpace(message))
        {
            Console.WriteLine("Message cannot be empty.");
            return;
        }

        try
        {
            await peerClient.SendAsync(message);
            Console.MarkupLine("[green4]Message sent.[/]");
            await Console.PromptAsync(new TextPrompt<string>("Continue").AllowEmpty(), cancellationToken);
            Console.Clear();
        }
        catch (Exception ex)
        {
            Console.MarkupLine("[red3]Failed to send message[/]");

            Logger.LogError(ex, "Couldn't send message.");
        }

    }
}
