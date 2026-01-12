using Spectre.Console;

namespace QuicPeer;

public interface IConsoleAccessor
{
    IAnsiConsole Console { get; }

    public IPrompt<T> SelectionPrompt<T>(IEnumerable<T> options) where T: notnull;
}
