using QuicPeer.Common;

namespace QuicPeer.Server;

public interface IPeersStore
{
    bool Contains(Certificate certificate);
}