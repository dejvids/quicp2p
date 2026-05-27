using NSubstitute;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Server;

namespace QuicPeer.Tests.Server;

public class ServerBaseTests
{
    private readonly CancellationTokenSource _cts = new(500);

    [Fact]
    public async Task should_listen_for_console_messages()
    {
        var messageQueue = Substitute.For<IMessageQueue<IClientMessage>>();
        var server = new TestServer(
            Substitute.For<IPeersStore>(),
            messageQueue);

        await server.StartAsync(_cts.Token);
        await server.Activated.WaitAsync(_cts.Token);

        messageQueue.Received().DequeueAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_not_start_listener_until_certificate_is_loaded()
    {
        var server = new TestServer(
            Substitute.For<IPeersStore>(),
            Substitute.For<IMessageQueue<IClientMessage>>())
            .WithCertificateLoader(()=> throw new Exception("Cannot load certificate"));

        var exception = await Record.ExceptionAsync(async () => await server.StartAsync(_cts.Token));

        Assert.NotNull(exception);
        Assert.False(server.IsListening);
    }

    [Fact]
    public async Task should_start_listener_if_certificate_is_loaded()
    {
        var server = new TestServer(
            Substitute.For<IPeersStore>(),
            Substitute.For<IMessageQueue<IClientMessage>>());

        await server.StartAsync(_cts.Token);

        Assert.True(server.IsListening);
    }

    [Fact]
    public async Task should_exit_if_msquic_is_notsupported()
    {
        var server = new TestServer(
            Substitute.For<IPeersStore>(),
            Substitute.For<IMessageQueue<IClientMessage>>())
            .WithMsQuicValidator(() => throw new NotSupportedException());

        await server.StartAsync(_cts.Token);

        Assert.False(server.Activated.IsCompletedSuccessfully);
        Assert.False(server.IsListening);
    }
}
