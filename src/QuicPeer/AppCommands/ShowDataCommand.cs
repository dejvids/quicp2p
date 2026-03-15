
using QuicPeer.Common.Messaging.ServerQueue;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class ShowDataCommand(ILogger<ShowDataCommand> logger, IConsoleAccessor consoleAccessor)
    : AppCommand<IEnumerable<TextReceived>>(logger, consoleAccessor)
{
    public override string CommandName => "Data";

    public override async ValueTask<CommandResult> Execute(IEnumerable<TextReceived> messages, CancellationToken cancellationToken)
    {
        var messagesList = messages.ToList();
        if (messagesList.Count == 0)
        {
            Console.WriteLine("Empty");
        }
        else
        {
            foreach (var message in messagesList)
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
