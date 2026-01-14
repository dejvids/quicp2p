using NSubstitute;
using Spectre.Console;

namespace QuicPeer.Tests.AppCommands;

public abstract  class AppCommandTestsBase : IDisposable
{
    private readonly CancellationTokenSource _cts = new (100);
    private bool _disposed;
    
    protected CancellationToken CancellationToken => _cts.Token;
    protected IConsoleAccessor ConsoleAccessor { get; } = Substitute.For<IConsoleAccessor>();

    protected AppCommandTestsBase()
    {
        ConsoleAccessor.Console.Returns(Substitute.For<IAnsiConsole>());
    }
    
    public void Dispose()
    {
        Dispose(disposing: true);

        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cts.Dispose();
            }

            _disposed = true;
        }
    }
    
    ~AppCommandTestsBase()
    {
        Dispose(false);
    }
}
    