using QuicPeer.Client;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class SendCommand : AppCommand<PeerClient>
{
    public SendCommand(ILogger<SendCommand> logger) : base(logger)
    {
    }

    public override string CommandName => "Send";

    protected override async ValueTask Execute(PeerClient peerClient, CancellationToken cancellationToken)
    {
        var message = AnsiConsole.Ask<string>("Enter the [green]message[/] to send:");
        if (string.IsNullOrWhiteSpace(message))
        {
            AnsiConsole.WriteLine("Message cannot be empty.");
            return;
        }

        try
        {
            await peerClient.SendAsync(message);
            AnsiConsole.MarkupLine("[green4]Message sent.[/]");
            await AnsiConsole.PromptAsync(new TextPrompt<string>("Continue").AllowEmpty());
            AnsiConsole.Clear();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red3]Failed to send message[/]");

            Logger.LogError(ex, "Couldn't send message.");
        }

    }
}
