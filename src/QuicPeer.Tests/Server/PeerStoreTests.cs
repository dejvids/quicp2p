using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.Options;
using QuicPeer.Server;

namespace QuicPeer.Tests.Server;

public class PeerStoreTests
{
    private const string CertificatesPath = "trusted";

    private readonly IDirectoryInfo _certsDirectory = Substitute.For<IDirectoryInfo>();
    private readonly IOptions<ServerOptions> _options = Substitute.For<IOptions<ServerOptions>>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public PeerStoreTests()
    {
        _fileSystem.Directory.CreateDirectory(CertificatesPath)
            .Returns(_certsDirectory);

        _options.Value.Returns(new ServerOptions()
        {
            ServerCertificate = new CertificateOptions(),
            TrustedCertsPath = CertificatesPath
        });
    }

    [Fact]
    public void should_load_installed_certificates_on_startup()
    {
        _ = new PeersStore(_fileSystem, _options, Substitute.For<ILogger<PeersStore>>());

        _fileSystem.Directory.Received(1).CreateDirectory(CertificatesPath);
        _certsDirectory.Received(1).EnumerateFiles();
    }
}