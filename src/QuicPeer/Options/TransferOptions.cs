namespace QuicPeer.Options;

public class TransferOptions
{
    public const string SectionName = "Transfer";

    public int BufferSize { get; init; } = 81_920; //Default: 81 920KB
}

public class ServerTransferOptions : TransferOptions
{
    public string DownloadsDirectory { get; init; } = "downloads";
}