using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Server;

public class CertificateValidator
{
    private readonly IFileSystem _fileSystem;
    private readonly ServerOptions _serverOptions;
    private readonly List<TrustedPeer> _trustedPeers;

    public CertificateValidator(IFileSystem fileSystem, IOptions<ServerOptions> serverOptions)
    {
        _fileSystem = fileSystem;
        _serverOptions = serverOptions.Value;
        _trustedPeers = LoadTrustedPeers();
    }

    public bool IsTrusted(X509Certificate certificate)
    {
        var fingerprint = certificate.GetCertHash(HashAlgorithmName.SHA256);
        return _trustedPeers.Any(t => t.CertFingerprint.SequenceEqual(fingerprint));
    }

    private List<TrustedPeer> LoadTrustedPeers()
    {
        var path = _serverOptions.TrustedCertsPath;
        var certsDirectory = _fileSystem.Directory.CreateDirectory(path);
        
        var trustedPeers = new List<TrustedPeer>();
        foreach (var file in certsDirectory.EnumerateFiles("*.crt"))
        {
            try
            {
                using var certicate = X509CertificateLoader.LoadCertificateFromFile(file.FullName);
                var fingerprint = certicate.GetCertHash(HashAlgorithmName.SHA256);
                var name = _fileSystem.Path.GetFileNameWithoutExtension(file.Name);
                trustedPeers.Add(new TrustedPeer(name, fingerprint));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return trustedPeers;
    }
}

class TrustedPeer(string name, byte[] certFingerprint)
{
    public string Name { get; } = name;
    public byte[] CertFingerprint { get; } = certFingerprint;
}