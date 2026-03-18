namespace QuicPeer.Common.Messaging.ClientQueue;

public class Unlocked(byte[] certificate, string password) : IClientMessage
{
    public byte[] Certificate { get; } = certificate;
    public string Password { get; } = password;
}