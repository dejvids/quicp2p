using Spectre.Console;

namespace QuicPeer;

public class ConsoleAccessor : IConsoleAccessor
{
    public IAnsiConsole Console => AnsiConsole.Console;

    public Task<bool> ConfirmAsync(string prompt, bool defaultValue = true, CancellationToken ct = default) =>
        Console.ConfirmAsync(prompt, defaultValue, ct);

    public IPrompt<T> SelectionPrompt<T>(IEnumerable<T> options) where T: notnull => 
        new SelectionPrompt<T>().AddChoices(options);
}
