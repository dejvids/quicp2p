using System.IO.Abstractions;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Options;

namespace QuicPeer.Server;

public sealed class PeersStore : IPeersStore, IDisposable
{
    private static readonly string[] CerFileExtensions = [".crt", ".pem"];
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;
    private readonly ServerOptions _serverOptions;
    private readonly List<Certificate> _trustedPeers;
    private bool _disposed;

    public PeersStore(IFileSystem fileSystem, IOptions<ServerOptions> serverOptions, ILogger<PeersStore> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _serverOptions = serverOptions.Value;
        _trustedPeers = LoadTrustedPeers();
    }

    public bool Contains(Certificate certificate)
    {
        var fingerprint = certificate.GetFingerprint();
        return _trustedPeers.Any(t => t.GetFingerprint().SequenceEqual(fingerprint));
    }

    private List<Certificate> LoadTrustedPeers()
    {
        var path = _serverOptions.TrustedCertsPath;
        var certsDirectory = _fileSystem.Directory.CreateDirectory(path);

        var trustedPeers = new List<Certificate>();
        foreach (var file in GetCertificateFiles(certsDirectory))
        {
            try
            {
                var cert = new Certificate(X509CertificateLoader.LoadCertificateFromFile(file.FullName));
                trustedPeers.Add(cert);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to load certificate from {File}", file.FullName);
            }
        }

        return trustedPeers;
    }

    private static IEnumerable<IFileInfo> GetCertificateFiles(IDirectoryInfo certsDirectory) =>
        certsDirectory.EnumerateFiles()
            .Where(file => CerFileExtensions.Contains(file.Extension, StringComparer.OrdinalIgnoreCase));

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        foreach (var certificate in _trustedPeers)
        {
            certificate.Dispose();
        }

        _trustedPeers.Clear();
        GC.SuppressFinalize(this);
    }
    
    ~PeersStore()
    {
        Dispose();
    }
}