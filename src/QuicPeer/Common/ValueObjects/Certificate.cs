using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Common.ValueObjects;

public class Certificate
{
    private X509Certificate Value { get; }
    public Certificate(X509Certificate value)
    {
        Value = value;
    }
    
    public bool IsNotExpired() =>
        !DateTime.TryParse(Value.GetExpirationDateString(), out var expirationDate) ||
        expirationDate > DateTime.UtcNow;
    
    public byte[] GetFingerprint() =>
        Value.GetCertHash(HashAlgorithmName.SHA256);
}