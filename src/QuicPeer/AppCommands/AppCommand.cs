using Spectre.Console;

namespace QuicPeer.AppCommands;

public abstract class AppCommand
{
    public abstract string CommandName { get; }

    protected ILogger Logger { get; }
    public IAnsiConsole Console { get; }

    protected AppCommand(ILogger logger, IAnsiConsole console)
    {
        Logger = logger;
        Console = console;
    }

    public abstract ValueTask Execute(CancellationToken cancellationToken);
}

public abstract class AppCommand<T> : AppCommand
{
    protected AppCommand(ILogger logger, IAnsiConsole console) 
        : base(logger, console)
    {
    }

    public abstract ValueTask Execute(T param, CancellationToken cancellationToken);

    public override ValueTask Execute(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
