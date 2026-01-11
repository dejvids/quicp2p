using Microsoft.Extensions.Options;
using QuicPeer.Client;
using QuicPeer.Options;

namespace QuicPeer.Tests;

public class PeerClientTests
{
    private IOptions<ClientOptions> options;

    [Fact]
    public void Test1()
    {
        System.Net.IPEndPoint endpoint = null;
        var peerClient = new PeerClient(options, endpoint);
    }
}
