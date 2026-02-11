using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Common;

public sealed class Certificate : IDisposable
{
    private bool _disposed;
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

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        
        _disposed = true;
        Value.Dispose();
        GC.SuppressFinalize(this);
    }
    
    ~Certificate()
    {
        Dispose();
    }
}