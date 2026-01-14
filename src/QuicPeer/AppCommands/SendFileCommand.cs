using QuicPeer.Client;
using Spectre.Console;
namespace QuicPeer.AppCommands;

public class SendFileCommand : AppCommand<IPeerClient>
{
    public SendFileCommand(ILogger<SendFileCommand> logger, IConsoleAccessor consoleAccessor) : base(logger, consoleAccessor)
    {
    }

    public override string CommandName => "Send file";
    public override async ValueTask Execute(IPeerClient peerClient, CancellationToken cancellationToken)
    {
        Console.Clear();

        try
        {
            var fileToSend = await GetFileAsync(cancellationToken);
            await Console.Status()
                .Spinner(Spinner.Known.Line)
                .StartAsync("Sending...", async _ => await peerClient.SendFileAsync(fileToSend));

            Console.MarkupLine("[green]:check_mark: File sent successfully. [/]");

        }
        catch (Exception ex)
        {
            Console.MarkupLine("[red]Couldn't send file[/]");
            Logger.LogError(ex, "Couldn't send file");
        }

        finally
        {
            await Console.PromptAsync(new TextPrompt<string>("Continue").AllowEmpty(), cancellationToken);
            Console.Clear();
        }
    }

    private async Task<FileInfo> GetFileAsync(CancellationToken cancellationToken)
    {
        var prompt = new TextPrompt<string>("Select path")
            .Validate(path =>
        {
            path = TrimPath(path);
            if (string.IsNullOrWhiteSpace(path))
            {
                return ValidationResult.Error("[red] Empty path[/]");
            }
            if (!File.Exists(path))
            {
                return ValidationResult.Error("[red] File does not exit[/]");
            }

            return ValidationResult.Success();
        });

        var filePath = await Console.PromptAsync(prompt, cancellationToken);

        return new FileInfo(TrimPath(filePath));
    }

    private static string TrimPath(string path) => path.Trim().Trim("\"").ToString();
}
