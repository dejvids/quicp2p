
using QuicPeer.Server.Commands;
using Spectre.Console;

namespace QuicPeer.AppCommands;

internal class ShowDataCommand : AppCommand<IEnumerable<MessageCommand>>
{
    public override string CommandName => "Data";

    protected override ValueTask Execute(IEnumerable<MessageCommand> messages, CancellationToken cancellationToken)
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
        AnsiConsole.Prompt(new TextPrompt<string>("Ok").AllowEmpty());
        AnsiConsole.Clear();

        return ValueTask.CompletedTask;
    }
}
