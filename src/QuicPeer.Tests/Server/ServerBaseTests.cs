using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Options;
using QuicPeer.Server;
using QuicPeer.Tests.Common;

namespace QuicPeer.Tests.Server;

public class ServerBaseTests
{
    [Fact]
    public async Task should_listen_for_console_messages()
    {
        var messageQueue = Substitute.For<IMessageQueue<IClientMessage>>();
        var server = new TestServer(Substitute.For<IOptions<ServerOptions>>(),
            Substitute.For<ILogger>(),
            Substitute.For<IPeersStore>(),
            messageQueue);

        var cts = new CancellationTokenSource(200);

        var exception = await Record.ExceptionAsync(async () => await server.StartAsync(cts.Token));
        
        Assert.IsType<OperationCanceledException>(exception, false);
        messageQueue.Received().DequeueAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_not_start_listener_until_certificate_is_loaded()
    {
        var server = new TestServer(Substitute.For<IOptions<ServerOptions>>(),
            Substitute.For<ILogger>(),
            Substitute.For<IPeersStore>(),
            Substitute.For<IMessageQueue<IClientMessage>>());

        var cts = new CancellationTokenSource(200);

        var exception = await Record.ExceptionAsync(async () => await server.StartAsync(cts.Token));
        
        Assert.IsType<OperationCanceledException>(exception, false);
        Assert.False(server.IsListening);
    }

    [Fact]
    public async Task should_start_listener_if_certificate_is_loaded()
    {
        var options = Substitute.For<IOptions<ServerOptions>>();
        options.Value.Returns(new ServerOptions
        {
            ServerCertificate = new CertificateOptions()
        });

        var messageQueue = Substitute.For<IMessageQueue<IClientMessage>>();
        messageQueue.DequeueAllAsync().ReturnsForAnyArgs(_ => AsyncEnumerable.Range(1, 1)
        .Select(_ => new Unlocked(Convert.FromBase64String(CertificateTests.Pfx), CertificateTests.Password)));
        var server = new TestServer(options,
            Substitute.For<ILogger>(),
            Substitute.For<IPeersStore>(),
            messageQueue);

        var cts = new CancellationTokenSource(200);

        await server.StartAsync(cts.Token);


        Assert.True(server.IsListening);
    }
}
