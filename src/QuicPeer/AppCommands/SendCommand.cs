using QuicPeer.Client;
using Spectre.Console;

namespace QuicPeer.AppCommands;

internal class SendCommand : AppCommand<PeerClient>
{
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
            AnsiConsole.Prompt(new TextPrompt<string>("Continue").AllowEmpty());
            AnsiConsole.Clear();
        }
        catch (Exception)
        {
            AnsiConsole.MarkupLine("[red3]Failed to send message[/]");
        }

    }
}
