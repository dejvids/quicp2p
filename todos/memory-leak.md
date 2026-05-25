# Memory & Allocation Audit — Outstanding Items

Findings from the initial scan of `src/QuicPeer/` (Tests excluded).
Original numbering preserved so cross-references stay stable.
Completed and omitted:
- Item 1 — `ConsoleApp._messages` unbounded growth (bounded `ServerMessageQueue` + direct drain in `ShowDataCommand`).
- Item 2 — `ConnectionContext._files` unbounded growth (`TryRemove` on consume).
- Item 3 — `PeerClient` `CancellationTokenSource` leak (constructor injection, single owner).
- Item 4 — `PeerClient._stopwatch` reused across sends (replaced with a per-call local `Stopwatch`; also resolves item 17).
- Item 5 — `Task.Factory.StartNew(async () => ...)` returning `Task<Task>` in `ConsoleApp.ShowMenu` (switched to `Task.Run` and moved the `try/catch` inside the lambda so faults are actually observed). `ServerBase.ListenConsoleMessages` and `PeerServer.OnPeerConnected` already used the async-aware `Task.Run` overload — no change needed there.
- Item 6 — `OnTextStreamOpened` per-stream allocation and missing oversize handling (switched to `ArrayPool<byte>.Shared.Rent(1000)` with `try/finally Return`; oversized payloads abort the stream and surface a `[TRUNCATED MESSAGE]` notice. Also resolves item 15.). **Outstanding sub-items:** the buffer is still drained with a single `ReadAsync` rather than a loop, so a QUIC short-read can mis-classify large payloads as small, and a multi-byte UTF-8 sequence split across reads can decode incorrectly. Metadata streams still rely on the JSON fitting in one read.

## High-impact

## Medium-impact

### 7. `PeersStore.Contains` recomputes SHA-256 on every call
[PeersStore.cs:26-30](../src/QuicPeer/Server/PeersStore.cs#L26) and
[Certificate.cs:62](../src/QuicPeer/Common/Certificate.cs#L62)

```csharp
return _trustedPeers.Any(t => t.GetFingerprint().SequenceEqual(fingerprint));
```
`GetFingerprint()` calls `X509Certificate2.GetCertHash(SHA256)` which
**recomputes the hash and allocates a new `byte[]` every invocation**.
With N trusted peers, every connection performs N+1 SHA-256s and N+1
array allocations.

**Fix:** cache the fingerprint as a precomputed `byte[]` (or `string`
hex) on `Certificate` at construction time, or store a
`HashSet<string>` keyed on hex fingerprint in `PeersStore`.

### 8. `Encoding.UTF8.GetBytes` + JSON string intermediate
[PeerClient.cs:52,94-95](../src/QuicPeer/Client/PeerClient.cs#L52)

```csharp
var payload = Encoding.UTF8.GetBytes(message);                                // SendAsync
var jsonPayload = System.Text.Json.JsonSerializer.Serialize(metadata);        // SendMetadata
var payload = Encoding.UTF8.GetBytes(jsonPayload);
```
Two allocations where one would do.

**Fix:** use `JsonSerializer.SerializeToUtf8Bytes(metadata)` to skip the
intermediate `string`. For `SendAsync`, `Encoding.UTF8.GetByteCount` +
`ArrayPool` rent + `GetBytes(message, span)` would also avoid the
per-call allocation if `SendAsync` is used in a tight loop.

### 9. `CheckSumProvider` reads the file twice on the receive path
[CheckSumProvider.cs:9,25](../src/QuicPeer/Common/CheckSumProvider.cs#L9)

`FilesReceiver.ReceiveFileAsync` calls `VerifyChecksum`, which calls
`GetChecksum`, which opens the file and SHA-256s the entire body — but
`Stream.CopyToAsync` already streamed the whole file once on receive.

**Fix:** compute the hash incrementally as the file is copied (using
`IncrementalHash`) to halve the I/O.

### 10. `Certificate` and `PeersStore` use a broken finalizer pattern
[Certificate.cs:65-80](../src/QuicPeer/Common/Certificate.cs#L65),
[PeersStore.cs:58-78](../src/QuicPeer/Server/PeersStore.cs#L58)

Both classes call `Dispose()` from the finalizer, and `Dispose()` then
touches managed references (`Value.Dispose()`,
`_trustedPeers[i].Dispose()`). Managed objects may already have been
finalized — accessing them from a finalizer is undefined.

**Fix:** delete both finalizers entirely (the wrapped
`X509Certificate2` already has its own), or implement the standard
`Dispose(bool disposing)` pattern that only releases unmanaged handles
when `disposing == false`.

### 11. `ConnectCommand._subCommands` linear scan per command
[ConnectCommand.cs:85](../src/QuicPeer/AppCommands/ConnectCommand.cs#L85)

`_subCommands.FirstOrDefault(c => c.CommandName == clientCommand)`
allocates a closure and walks an array on every menu pick.

**Fix:** build a `Dictionary<string, AppCommand<IPeerClient>>` in the
constructor, like `ConsoleApp` does at
[ConsoleApp.cs:44](../src/QuicPeer/ConsoleApp.cs#L44).

## Low-impact / cosmetic

### 12. `Certificate.IsExpired` parses a string instead of using `NotAfter`
[Certificate.cs:57-60](../src/QuicPeer/Common/Certificate.cs#L57)

`Value.GetExpirationDateString()` allocates and `DateTimeOffset.TryParse`
does culture-sensitive parsing. `Value.NotAfter` is a `DateTime`
directly — no allocation, no culture risk.

### 13. `ShowDataCommand` allocates an extra list
[ShowDataCommand.cs:14](../src/QuicPeer/AppCommands/ShowDataCommand.cs#L14)

`var messagesList = messages.ToList()`. The caller already passes
`ConcurrentQueue<TextReceived>`. Iterate it directly and check `IsEmpty`
instead.

### 14. Misleading `BufferSize` comment
[TransferOptions.cs:7](../src/QuicPeer/Options/TransferOptions.cs#L7)

`81_920 //Default: 81 920KB` — the value is 80 KiB (bytes), not
81 920 KB. The number is intentionally below the LOH threshold
(~85,000 bytes), which is correct, but the comment misrepresents it by
~1000×.

### 16. `EndpointParser.TryParseDnsEndpoint` returns `iPAddresses.Last()`
[EndpointParser.cs:51](../src/QuicPeer/Client/EndpointParser.cs#L51)

Not a memory issue, but `.Last()` on an array allocates an enumerator
path; use `iPAddresses[^1]` for indexed access. Splitting on `":"` also
breaks for IPv6 hostnames-with-port forms.

### 18. `RenameToInvalid` is dead code
[FilesReceiver.cs:59-62](../src/QuicPeer/Server/FilesReceiver.cs#L59)

Not a memory issue, but `_fileSystem.Path.ChangeExtension(...)` returns
a new string and discards it; nothing actually renames. The corrupt
file is left with the `.download` extension.

## Notes

- No LOH allocations were found — the 80 KB transfer buffer is
  intentionally just under the 85 KB threshold.
- Item 7 is the biggest hot-path allocation win.
