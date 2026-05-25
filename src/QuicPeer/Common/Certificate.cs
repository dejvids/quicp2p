using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using QuicPeer.Options;

namespace QuicPeer.Common;

public sealed class Certificate : IDisposable
{
    private bool _disposed;
    private byte[] _fingerprint;
    private X509Certificate2 Value { get; }

    public byte[] Fingerprint => _fingerprint ??= GetFingerprint();

    public Certificate(X509Certificate2 value)
    {
        Value = value;
    }

    public Certificate(CertificateOptions options, TimeProvider timeProvider)
    {
        Value = GenerateSelfSigned(options, timeProvider);
    }

    public byte[] GetPfx() => 
        Value.Export(X509ContentType.Pfx);
    
    public string GetPem() =>
        Value.ExportCertificatePem();

    public string GetPrivateKey(string passphrase) =>
        Value.GetECDsaPrivateKey()!
            .ExportEncryptedPkcs8PrivateKeyPem(passphrase,
                new PbeParameters(PbeEncryptionAlgorithm.Aes128Cbc, HashAlgorithmName.SHA256, 3));

    private static X509Certificate2 GenerateSelfSigned(CertificateOptions options, TimeProvider timeProvider)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var distinguishedName = new X500DistinguishedName($"CN={options.CommonName}");

        var request = new CertificateRequest(
            distinguishedName,
            ecdsa,
            HashAlgorithmName.SHA256);

        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DataEncipherment |
                X509KeyUsageFlags.KeyEncipherment |
                X509KeyUsageFlags.DigitalSignature,
                critical: false));

        var notAfter = timeProvider.GetUtcNow().Add(options.Lifespan);
        var notBefore = timeProvider.GetUtcNow().AddDays(-1);

        return request.CreateSelfSigned(notBefore, notAfter);
    }

    public bool IsExpired(TimeProvider timeProvider) =>
        DateTimeOffset.TryParse(Value.GetExpirationDateString(), CultureInfo.CurrentCulture,
            DateTimeStyles.None, out var expirationDate) &&
        timeProvider.GetUtcNow() > expirationDate;

    private byte[] GetFingerprint() =>
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