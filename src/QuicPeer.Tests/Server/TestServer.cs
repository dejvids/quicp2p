using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Options;
using QuicPeer.Server;
using QuicPeer.Tests.Common;
using System.Net.Quic;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Tests.Server;

public class TestServer : ServerBase
{
    private readonly X509Certificate2 _testCertificate = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(CertificateTests.Base64));
    private Func<X509Certificate2> _certificateLoader;
    private Action _msQuicInstallationCheck;

    public bool IsListening { get; private set; }

    public TestServer(
        IPeersStore peersStore,
        IMessageQueue<IClientMessage> messageQueue)
        : base(GetDefaultOptions(), Substitute.For<ILogger<TestServer>>(), peersStore, messageQueue)
    {
        _certificateLoader = () => _testCertificate;
        _msQuicInstallationCheck = () => { /* Supported by default */ };
    }
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await ExecuteAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunServerAsync(stoppingToken);
    }

    protected override Task RunServerInternal(QuicListenerOptions options, CancellationToken stoppingToken)
    {
        IsListening = true;

        return Task.CompletedTask;
    }

    protected override void EnsureProtocolSupport()
    {
        _msQuicInstallationCheck();
    }

    protected override Task<X509Certificate2> LoadCertificate(CancellationToken stoppingToken)
    {
        return Task.FromResult(_certificateLoader());
    }

    private static IOptions<ServerOptions> GetDefaultOptions()
    {

        var options = Substitute.For<IOptions<ServerOptions>>();
        options.Value.Returns(new ServerOptions
        {
            ServerCertificate = new CertificateOptions()
        });

        return options;
    }

    public TestServer WithCertificateLoader(Func<X509Certificate2> value)
    {
        _certificateLoader = value;

        return this;
    }

    public TestServer WithMsQuicValidator(Action msQuicValidator)
    {
        _msQuicInstallationCheck = msQuicValidator;

        return this;
    }
}
