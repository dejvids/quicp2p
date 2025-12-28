namespace QuicPeer.Options;

public class PeerConnectionOptions
{
    public int MaxInboundUnidirectionalStreams { get; set; } = 10;
    public int MaxInboundBidirectionalStreams { get; set; } = 10;
    public int IdleTimeout { get; set; } = 30;
    public int DefaultStreamErrorCode { get; set; } = 0xA;
    public int DefaultCloseErrorCode { get; set; } = 0xB;
}
