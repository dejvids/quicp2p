namespace QuicPeer.Common.Messaging.ServerQueue;

public record TextReceived(string From, string Message, TimeOnly Time) : IServerMessage;
