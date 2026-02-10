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

    public bool IsExpired(TimeProvider timeProvider) =>
        DateTime.TryParse(Value.GetExpirationDateString(), CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var expirationDate) &&
        timeProvider.GetUtcNow() > expirationDate;

    public byte[] GetFingerprint() =>
        Value.GetCertHash(HashAlgorithmName.SHA256);
}