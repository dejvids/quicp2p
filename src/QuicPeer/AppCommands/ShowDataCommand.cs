using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ServerQueue;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class ShowDataCommand(
    ILogger<ShowDataCommand> logger,
    IConsoleAccessor consoleAccessor,
    IMessageQueue<IServerMessage> messageQueue)
    : AppCommand(logger, consoleAccessor)
{
    public override string CommandName => "Data";

    public override async ValueTask<CommandResult> Execute(CancellationToken cancellationToken)
    {
        var anyShown = false;
        while (messageQueue.TryDequeue(out var item))
        {
            if (item is not TextReceived text)
            {
                continue;
            }

            anyShown = true;
            Console.MarkupLine("Message at [yellow]{0}[/] from: [yellow]{1}[/]",
                text.Time.ToString("HH:mm:ss"), text.From);
            Console.MarkupLine("\t [blue]{0}[/]", text.Message);
            Console.WriteLine();
        }

        if (!anyShown)
        {
            Console.WriteLine("Empty");
        }

        await Console.PromptAsync(new TextPrompt<string>("Ok").AllowEmpty(), cancellationToken);
        Console.Clear();

        return CommandResult.Success;
    }
}
