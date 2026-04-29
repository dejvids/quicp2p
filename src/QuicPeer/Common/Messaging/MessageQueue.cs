using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;

namespace QuicPeer.Common.Messaging;

public abstract class MessageQueue<T> : IMessageQueue<T>
{
    private readonly Channel<T> _channel;

    protected MessageQueue() : this(Channel.CreateUnbounded<T>())
    {
    }

    protected MessageQueue(Channel<T> channel)
    {
        _channel = channel;
    }

    public IAsyncEnumerable<T> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public ValueTask EnqueueAsync(T message)
    {
        return _channel.Writer.WriteAsync(message);
    }

    public bool TryDequeue([MaybeNullWhen(false)] out T message)
    {
        return _channel.Reader.TryRead(out message);
    }
}
