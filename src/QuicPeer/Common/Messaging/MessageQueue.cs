using System.Threading.Channels;

namespace QuicPeer.Common.Messaging;

public abstract class MessageQueue<T> : IMessageQueue<T>
{
    private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();
    public IAsyncEnumerable<T> DequeueAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public ValueTask EnqueueAsync(T message)
    {
        return _channel.Writer.WriteAsync(message);
    }
}