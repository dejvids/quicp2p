using System.IO.Abstractions;
using System.Net.Quic;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common.Exceptions;
using Spectre.Console;
namespace QuicPeer.AppCommands;

public class SendFileCommand : AppCommand<IPeerClient>
{
    private readonly IFileSystem _fileSystem;
    public SendFileCommand(ILogger<SendFileCommand> logger, IConsoleAccessor consoleAccessor, IFileSystem fileSystem) 
        : base(logger, consoleAccessor)
    {
        _fileSystem = fileSystem;
    }

    public override string CommandName => "Send file";
    public override async ValueTask<CommandResult> Execute(IPeerClient peerClient, CancellationToken cancellationToken)
    {
        Console.Clear();

        try
        {
            var fileToSend = await GetFileAsync(cancellationToken);
            var resul = await Console.Status()
                .Spinner(Spinner.Known.Line)
                .StartAsync("Sending...", async _ => await peerClient.SendFileAsync(fileToSend));

            Console.MarkupLine("[green]:check_mark: File sent successfully. [/]");
            Logger.LogInformation("File {FileName} sent successfully in {Time}", fileToSend.FullName, resul.ElapsedTime);

        }
        catch (QuicException ex) when (ex.IsConnectionError())
        {
            Console.MarkupLine("[red]Peer disconnected.[/]");
            Logger.LogError(ex, "Server disconnected.");
            return CommandResult.Error;
        }
        catch (OperationCanceledException)
        {
            Console.MarkupLine("[red]Operation canceled[/]");
            return CommandResult.Fail;
        }
        catch (Exception ex)
        {
            Console.MarkupLine("[red]Couldn't send file[/]");
            Logger.LogError(ex, "Couldn't send file");
            
            return CommandResult.Fail;
        }

        finally
        {
            await Console.PromptAsync(new TextPrompt<string>("Continue").AllowEmpty(), cancellationToken);
            Console.Clear();
        }
        
        return CommandResult.Success;
    }

    private async Task<IFileInfo> GetFileAsync(CancellationToken cancellationToken)
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

        return _fileSystem.FileInfo.New(TrimPath(filePath));
    }

    private static string TrimPath(string path) => path.Trim().Trim("\"").ToString();
}
