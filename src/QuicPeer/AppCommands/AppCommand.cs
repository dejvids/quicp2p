using Spectre.Console;

namespace QuicPeer.AppCommands;

public abstract class AppCommand(ILogger logger, IConsoleAccessor consoleAccessor)
{
    public abstract string CommandName { get; }

    protected ILogger Logger { get; } = logger;
    protected IConsoleAccessor ConsoleAccessor { get; } = consoleAccessor;
    protected IAnsiConsole Console => ConsoleAccessor.Console;

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
