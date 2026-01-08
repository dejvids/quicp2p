namespace QuicPeer.AppCommands;

internal abstract class AppCommand
{
    public abstract string CommandName { get; }

    protected abstract ValueTask Execute(CancellationToken cancellationToken);

    public async ValueTask Start(CancellationToken cancellationToken)
    {
        await Execute(cancellationToken);
    }
}

internal abstract class AppCommand<T> : AppCommand
{
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
