using System.IO.Abstractions;
using System.Security.Cryptography;
using QuicPeer.Common.Exceptions;

namespace QuicPeer.Common;

public class ChecksumProvider : IChecksumProvider
{
    public string GetChecksum(IFileInfo file)
    {
        return !file.Exists 
            ? throw new FileNotFoundException("File not found", file.FullName) 
            : Convert.ToHexString(ComputeHashAsync(file));
    }

    private static byte[] ComputeHashAsync(IFileInfo file)
    {
        using var sha256 = SHA256.Create();
        using var fileStream = file.OpenRead();
        fileStream.Position = 0;

        return sha256.ComputeHash(fileStream);
    }

    public void VerifyChecksum(IFileInfo file, string checksum)
    {
        var actualChecksum = GetChecksum(file);

        if (!actualChecksum.Equals(checksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new DataIntegrityException($"Checksum mismatch of file {file.FullName}");
        }
    }
}