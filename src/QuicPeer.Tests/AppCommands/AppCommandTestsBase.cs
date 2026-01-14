using NSubstitute;
using Spectre.Console;

namespace QuicPeer.Tests.AppCommands;

public abstract  class AppCommandTestsBase : IDisposable
{
    private readonly CancellationTokenSource _cts = new (100);
    
    protected CancellationToken CancellationToken => _cts.Token;
    protected IConsoleAccessor ConsoleAccessor { get; } = Substitute.For<IConsoleAccessor>();

    protected AppCommandTestsBase()
    {
        ConsoleAccessor.Console.Returns(Substitute.For<IAnsiConsole>());
    }

    public void Dispose()
    {
        _cts.Dispose();
    }
}
    