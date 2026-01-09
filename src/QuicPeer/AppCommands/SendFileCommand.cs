using QuicPeer.Client;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class SendFileCommand : AppCommand<PeerClient>
{
    public SendFileCommand(ILogger<SendFileCommand> logger) : base(logger)
    {
    }

    public override string CommandName => "Send file";
    protected override async ValueTask Execute(PeerClient peerClient, CancellationToken cancellationToken)
    {
        AnsiConsole.Clear();

        try
        {
            var fileToSend = await GetFileAsync(cancellationToken);
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Line)
                .StartAsync("Sending...", async ctx => await peerClient.SendAsync(fileToSend));

            AnsiConsole.MarkupLine("[green]:check_mark: File sent successfully. [/]");

        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine("[red]Couldn't send file[/]");
            Logger.LogError(ex, "Couldn't send file");
        }

        finally
        {
            await AnsiConsole.PromptAsync(new TextPrompt<string>("Continue").AllowEmpty(), cancellationToken);
            AnsiConsole.Clear();
        }
    }

    private static async Task<FileInfo> GetFileAsync(CancellationToken cancellationToken)
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

        var filePath = await AnsiConsole.PromptAsync(prompt, cancellationToken);

        return new FileInfo(TrimPath(filePath));
    }

    private static string TrimPath(string path) => path.Trim().Trim("\"").ToString();
}
