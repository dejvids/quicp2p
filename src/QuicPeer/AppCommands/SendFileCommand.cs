using QuicPeer.Client;
using Spectre.Console;

namespace QuicPeer.AppCommands;

internal class SendFileCommand : AppCommand<PeerClient>
{
    public override string CommandName => "Send file";
    protected override async ValueTask Execute(PeerClient peerClient, CancellationToken cancellationToken)
    {
        AnsiConsole.Clear();
        var fileToSend = GetFile();

        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Line)
                .StartAsync("Sending...", async ctx => await peerClient.SendAsync(fileToSend));

            AnsiConsole.MarkupLine("[green]:check_mark: File sent successfully. [/]");

        }
        catch (Exception)
        {
            AnsiConsole.MarkupLine("[red]Couldn't send file[/]");
            throw;
        }

        finally
        {
            AnsiConsole.Prompt(new TextPrompt<string>("Continue").AllowEmpty());
            AnsiConsole.Clear();
        }
    }

    private static FileInfo GetFile()
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

        var filePath = AnsiConsole.Prompt(prompt);

        return new FileInfo(TrimPath(filePath));
    }

    private static string TrimPath(string path) => path.Trim().Trim("\"").ToString();
}
