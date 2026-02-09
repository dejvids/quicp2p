using System.IO.Abstractions;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Options;

namespace QuicPeer.Server;

public class PeersStore : IPeersStore
{
    private sealed class TrustedPeer(string name, byte[] certFingerprint)
    {
        public string Name { get; } = name;
        public byte[] CertFingerprint { get; } = certFingerprint;
    }

    private static readonly string[] CerFileExtensions = [".crt", ".pem"];
    private readonly IFileSystem _fileSystem;
    private readonly ServerOptions _serverOptions;
    private readonly List<TrustedPeer> _trustedPeers;
    private readonly HashAlgorithmName _hashAlgorithmName = HashAlgorithmName.SHA256;

    public PeersStore(IFileSystem fileSystem, IOptions<ServerOptions> serverOptions)
    {
        _fileSystem = fileSystem;
        _serverOptions = serverOptions.Value;
        _trustedPeers = LoadTrustedPeers();
    }

    public bool Contains(Certificate certificate)
    {
        var fingerprint = certificate.GetFingerprint();
        return _trustedPeers.Any(t => t.CertFingerprint.SequenceEqual(fingerprint));
    }
    
    private List<TrustedPeer> LoadTrustedPeers()
    {
        var path = _serverOptions.TrustedCertsPath;
        var certsDirectory = _fileSystem.Directory.CreateDirectory(path);

        var trustedPeers = new List<TrustedPeer>();
        foreach (var file in certsDirectory.EnumerateFiles()
                     .Where(file => CerFileExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase)))
        {
            try
            {
                using var cert = X509CertificateLoader.LoadCertificateFromFile(file.FullName);
                var fingerprint = cert.GetCertHash(_hashAlgorithmName);
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