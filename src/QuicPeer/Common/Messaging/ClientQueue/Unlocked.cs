namespace QuicPeer.Common.Messaging.ClientQueue;

public class Unlocked(byte[] certificate) : IClientMessage
{
    public byte[] Certificate { get; } = certificate;
}