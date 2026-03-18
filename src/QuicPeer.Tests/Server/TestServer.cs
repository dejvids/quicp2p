using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Options;
using QuicPeer.Server;
using System.Net.Quic;

namespace QuicPeer.Tests.Server;

public class TestServer : ServerBase
{
    public bool IsListening { get; private set; }
    public TestServer(IOptions<ServerOptions> serverOptions,
        ILogger logger,
        IPeersStore peersStore,
        IMessageQueue<IClientMessage> messageQueue) 
        : base(serverOptions, logger, peersStore, messageQueue)
    {
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return RunServerAsync(stoppingToken);
    }

    protected override Task RunServerInternal(QuicListenerOptions options, CancellationToken stoppingToken)
    {
        IsListening = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            Thread.SpinWait(1);
        }

        return Task.CompletedTask;
    }
}
