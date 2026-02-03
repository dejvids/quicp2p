
using QuicPeer.Server.Commands;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class ShowDataCommand : AppCommand<IEnumerable<MessageCommand>>
{
    public ShowDataCommand(ILogger<ShowDataCommand> logger, IConsoleAccessor consoleAccessor) : base(logger, consoleAccessor)
    {
    }

    public override string CommandName => "Data";

    public override async ValueTask<CommandResult> Execute(IEnumerable<MessageCommand> messages, CancellationToken cancellationToken)
    {
        if (!messages.Any())
        {
            Console.WriteLine("Empty");
        }
        else
        {
            foreach (var message in messages)
            {
                Console.MarkupLine("Message at [yellow]{0}[/] from: [yellow]{1}[/]", message.Time.ToString("HH:mm:ss"), message.From);
                Console.MarkupLine("\t [blue]{0}[/]", message.Message);
                Console.WriteLine();
            }
        }

        await Console.PromptAsync(new TextPrompt<string>("Ok").AllowEmpty(), cancellationToken);
        Console.Clear(); 
        
        return CommandResult.Success;
    }
}
