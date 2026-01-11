using Spectre.Console;

namespace QuicPeer;

public interface IConsoleAccessor
{
    IAnsiConsole Console { get; }
}
