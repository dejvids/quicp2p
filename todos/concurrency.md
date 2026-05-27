# Concurrency Issues

## 1. File transfer: body stream may be processed before metadata is registered

**Where:**
[ConnectionManager.HandleStream](../src/QuicPeer/Server/ConnectionManager.cs#L32),
[ConnectionContext.GetFileMetadata](../src/QuicPeer/Server/ConnectionContext.cs#L34),
[PeerClient.SendFileAsync](../src/QuicPeer/Client/PeerClient.cs#L74)

**Problem:** Body and metadata streams are accepted and dispatched
concurrently via `Task.Run`. If the body-stream task calls
`GetFileMetadata(stream.Id)` before `OnTextStreamOpened` has registered
the metadata, lookup returns `null` and the body is silently discarded.
Sender sees a successful send. Failure is non-deterministic and
unlogged.
