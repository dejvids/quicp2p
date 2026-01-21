namespace QuicPeer.Options;

public class FilesReceiverOptions
{
    public const string SectionName = nameof(ServerOptions.FilesReceiver);
    public string DownloadsDirectory { get; set; } = "downloads";
}