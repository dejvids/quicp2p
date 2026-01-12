using Spectre.Console;

namespace QuicPeer;

public class ConsoleAccessor : IConsoleAccessor
{
    public IAnsiConsole Console => AnsiConsole.Console;

    public IPrompt<T> SelectionPrompt<T>(IEnumerable<T> options) where T: notnull => 
        new SelectionPrompt<T>().AddChoices(options);
}
