using Spectre.Console;

namespace QuicPeer.AppCommands;

public abstract class AppCommand
{
    public abstract string CommandName { get; }

    protected ILogger Logger { get; }
    protected IConsoleAccessor ConsoleAccessor { get; }
    protected IAnsiConsole Console => ConsoleAccessor.Console;

    protected AppCommand(ILogger logger, IConsoleAccessor consoleAccessor)
    {
        Logger = logger;
        ConsoleAccessor = consoleAccessor;
    }

    public abstract ValueTask<CommandResult> Execute(CancellationToken cancellationToken);
}

public abstract class AppCommand<T> : AppCommand
{
    protected AppCommand(ILogger logger, IConsoleAccessor consoleAccessor) 
        : base(logger, consoleAccessor)
    {
    }

    public abstract ValueTask<CommandResult> Execute(T param, CancellationToken cancellationToken);

    public override ValueTask<CommandResult> Execute(CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(CommandResult.Success);
    }
}
