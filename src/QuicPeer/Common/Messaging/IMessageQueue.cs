namespace QuicPeer.Common.Messaging;

public interface IMessageQueue<T>
{
    ValueTask EnqueueAsync(T message);
    IAsyncEnumerable<T> DequeueAllAsync(CancellationToken cancellationToken = default);
}
