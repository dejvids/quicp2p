namespace QuicPeer.AppCommands;

public abstract class AppCommand
{
    public abstract string CommandName { get; }

    protected ILogger Logger { get; }
    protected AppCommand(ILogger logger)
    {
        Logger = logger;
    }

    protected abstract ValueTask Execute(CancellationToken cancellationToken);

    public async ValueTask Start(CancellationToken cancellationToken)
    {
        await Execute(cancellationToken);
    }
}

public abstract class AppCommand<T> : AppCommand
{
    protected AppCommand(ILogger logger) 
        : base(logger)
    {
    }

    protected abstract ValueTask Execute(T param, CancellationToken cancellationToken);

    public async ValueTask Start(T param, CancellationToken cancellationToken)
    {
        await Execute(param, cancellationToken);
    }

    protected override ValueTask Execute(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
