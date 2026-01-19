namespace QuicPeer.Common;

public interface IChecksumProvider
{
    string GetChecksum(FileInfo file);
    void VerifyChecksum(FileInfo file, string checksum);
}