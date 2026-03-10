namespace QuicPeer.AppCommands;

public class Unlocked(byte[] certificate) : IConsoleMessage
{
    public byte[] Certificate { get; } = certificate;
}