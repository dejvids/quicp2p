using System.Diagnostics.CodeAnalysis;

namespace QuicPeer.Common.Messaging;

public interface IMessageQueue<T>
{
    ValueTask EnqueueAsync(T message);
    IAsyncEnumerable<T> DequeueAllAsync(CancellationToken cancellationToken = default);
    bool TryDequeue([MaybeNullWhen(false)] out T message);
}
