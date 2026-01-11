using Spectre.Console;

namespace QuicPeer;

public class ConsoleAccessor : IConsoleAccessor
{
    public IAnsiConsole Console => AnsiConsole.Console;
}
