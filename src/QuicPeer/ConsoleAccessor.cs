using Spectre.Console;

namespace QuicPeer;

public class ConsoleAccessor : IConsoleAccessor
{
    public IAnsiConsole Console => AnsiConsole.Console;

    public IPrompt<string> PasswordPrompt(string prompt) => 
        new TextPrompt<string>(prompt).Secret();

    public IPrompt<string> ConfirmationPrompt(string prompt = "Continue") => 
        new TextPrompt<string>(prompt).AllowEmpty();

    public Task<bool> ConfirmAsync(string prompt, bool defaultValue = true, CancellationToken ct = default) =>
        Console.ConfirmAsync(prompt, defaultValue, ct);

    public IPrompt<T> SelectionPrompt<T>(IEnumerable<T> options) where T: notnull => 
        new SelectionPrompt<T>().AddChoices(options);

    public IPrompt<T> TextPrompt<T>(string prompt) where T : notnull => 
        new TextPrompt<T>(prompt);

    public Task<T> SpinnerAsync<T>(string prompt, Task<T> task, CancellationToken ct = default) => 
        Console.Status()
               .Spinner(Spinner.Known.Line)
               .StartAsync(prompt, async _ => await Task.Run(async () => await task, ct));
}
