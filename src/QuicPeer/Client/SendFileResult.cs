namespace QuicPeer.Client;

public class SendFileResult(TimeSpan elapsedTime)
{
    public static SendFileResult Empty { get; } = new (TimeSpan.Zero);
    public TimeSpan ElapsedTime { get; } = elapsedTime;
}