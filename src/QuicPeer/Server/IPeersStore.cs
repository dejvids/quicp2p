using QuicPeer.Common.ValueObjects;

namespace QuicPeer.Server;

public interface IPeersStore
{
    bool Contains(Certificate certificate);
}