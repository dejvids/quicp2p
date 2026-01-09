
using QuicPeer.Server.Commands;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class ShowDataCommand : AppCommand<IEnumerable<MessageCommand>>
{
    public ShowDataCommand(ILogger<ShowDataCommand> logger) : base(logger)
    {
    }

    public override string CommandName => "Data";

    protected override async ValueTask Execute(IEnumerable<MessageCommand> messages, CancellationToken cancellationToken)
    {
        if (!messages.Any())
        {
            AnsiConsole.WriteLine("Empty");
        }
        else
        {
            foreach (var message in messages)
            {
                AnsiConsole.MarkupLine("Message at [yellow]{0}[/] from: [yellow]{1}[/]", message.Time.ToString("HH:mm:ss"), message.From);
                AnsiConsole.MarkupLine("\t [blue]{0}[/]", message.Message);
                AnsiConsole.WriteLine();
            }
        }

        await AnsiConsole.PromptAsync(new TextPrompt<string>("Ok").AllowEmpty(), cancellationToken);
        AnsiConsole.Clear();
    }
}
