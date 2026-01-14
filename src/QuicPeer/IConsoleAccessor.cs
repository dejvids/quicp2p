using Spectre.Console;

namespace QuicPeer;

public interface IConsoleAccessor
{
    IAnsiConsole Console { get; }

    public IPrompt<T> SelectionPrompt<T>(IEnumerable<T> options) where T: notnull;
    public IPrompt<T> TextPrompt<T>(string prompt) where T: notnull;
    public Task<bool> ConfirmAsync(string prompt, bool defaultValue = true, CancellationToken ct = default);
    public Task<T> SpinnerAsync<T>(string prompt, Task<T> task, CancellationToken ct = default);
}
