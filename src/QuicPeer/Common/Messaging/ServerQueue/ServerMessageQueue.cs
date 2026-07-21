using System.Threading.Channels;

namespace QuicPeer.Common.Messaging.ServerQueue;

public class ServerMessageQueue() : MessageQueue<IServerMessage>(
    Channel.CreateBounded<IServerMessage>(new BoundedChannelOptions(500)
    {
        FullMode = BoundedChannelFullMode.DropOldest
    }));
