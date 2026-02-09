using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Common;

public class Certificate
{
    private X509Certificate Value { get; }

    public Certificate(X509Certificate value)
    {
        Value = value;
    }

    public bool IsNotExpired() =>
        !DateTime.TryParse(Value.GetExpirationDateString(), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var expirationDate) ||
        expirationDate > DateTime.UtcNow;

    public byte[] GetFingerprint() =>
        Value.GetCertHash(HashAlgorithmName.SHA256);
}