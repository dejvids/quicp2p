using System.IO.Abstractions;

namespace QuicPeer.Common;

public interface IChecksumProvider
{
    string GetChecksum(IFileInfo file);
    void VerifyChecksum(IFileInfo file, string checksum);
}