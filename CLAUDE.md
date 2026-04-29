# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & test

The solution lives under `src/` and targets **.NET 10**. All commands are run from the repo root.

```bash
dotnet build -c Release ./src
dotnet test ./src/QuicPeer.Tests/ --no-build -c Release
dotnet test ./src/QuicPeer.Tests/ --filter "FullyQualifiedName~ServerBaseTests"   # single class
dotnet test ./src/QuicPeer.Tests/ --filter "DisplayName~SendFileAsync_returns_empty_when_no_connection"  # single test
dotnet run --project ./src/QuicPeer                                                # run the peer
```

CI (`.github/workflows/build.yml`) runs the same `dotnet build` / `dotnet test` pair wrapped in a SonarCloud (`dejvids_quicp2p` / `dawidsurys`) scan with OpenCover coverage. Test framework is **xUnit + NSubstitute**.

QUIC requires **msquic** to be present on the host. `ServerBase.EnsureProtocolSupport()` throws `NotSupportedException` if `QuicListener.IsSupported` / `QuicConnection.IsSupported` is false — on a fresh dev box this is the most common reason a server appears to start and immediately log an error instead of listening.

## Architecture

QuicPeer is a single-process **peer**: every instance runs both a QUIC server (accepts inbound connections, receives text + files) and a QUIC client (initiated from the interactive console). Configuration is in `src/QuicPeer/appsettings.json` with sections `Server`, `Client`, `Certificate`, `Transfer`; see `Options/OptionsExtensions.cs` for the binding rules — `Certificate` is shared into both `ServerOptions.ServerCertificate` and `ClientOptions.ClientCertificate`, and `Transfer` falls back into both server and client transfer settings if the per-side section is absent.

### Hosted services and the unlock handoff

`Program.cs` registers two `IHostedService`s on the generic host:

- `PeerServer` (`Server/PeerServer.cs`) — QUIC listener. On startup it does **not** have the certificate yet; `ServerBase.RunServerAsync` awaits a `TaskCompletionSource<X509Certificate2>` (`CertificateLoaded`) that is completed only after the user types the certificate password in the console. With autorestart on transient errors (`Options.RestartAttempts` / `RestartInterval`).
- `ConsoleApp` (`ConsoleApp.cs`) — Spectre.Console interactive menu. First action is always `UnlockCommand`: it creates the self-signed cert if missing (`Common/Certificate.cs`), loads the PEM + encrypted private key, exports a PFX in memory, and **enqueues an `Unlocked` message** onto `IMessageQueue<IClientMessage>`. The server's `ListenConsoleMessages` loop drains that queue and resolves `CertificateLoaded`. If unlock fails, the console asks the host to stop — the server never starts listening.

This means **the server cannot be tested or driven without the unlock step happening first**; tests fake it by completing the queue or constructing `ServerBase` subclasses directly (see `QuicPeer.Tests/Server/TestServer.cs`).

### The two message queues

`Common/Messaging/MessageQueue.cs` is a thin wrapper over an unbounded `System.Threading.Channels.Channel<T>`. There are two singletons:

- `IMessageQueue<IClientMessage>` — **console → server**. Carries `Unlocked(byte[] certificate, string password)`.
- `IMessageQueue<IServerMessage>` — **server → console**. Carries `TextReceived` (peer text) which `ConsoleApp.ReadServerCommands` enqueues into a local `ConcurrentQueue` for the `ShowDataCommand` to display.

Both queues are unbounded and never closed; cancellation propagates via the host's stopping token.

### App-commands menu (keyed DI)

`AppCommands/AppCommandsExtensions.cs` registers commands as **keyed singletons** under two menu keys:

- `ConsoleApp.MainMenu` — `ConnectCommand`, `ShowDataCommand` (top-level menu).
- `ConnectCommand.ConnectMenu` — `SendCommand`, `SendFileCommand` (only available after a connection is established).

`UnlockCommand` is registered un-keyed because it is run once at startup, not from the menu. When adding a new command, register it under the correct menu key — `ConsoleApp` enumerates `[FromKeyedServices(MainMenu)] IEnumerable<AppCommand>` to build the prompt.

### File transfer protocol

A single QUIC connection multiplexes three stream kinds — see `PeerClient.SendFileAsync` (`Client/PeerClient.cs`) and `ConnectionManager` (`Server/ConnectionManager.cs`):

1. **Bidirectional text stream** — UTF-8 message. Both plain text and the file metadata header travel on this stream type. The server tries to JSON-deserialize every incoming text payload as `FileMetadata` (`Common/Dto/FileMetadata.cs`); if it parses and `FileSize > 0`, it is treated as a metadata header and the server replies with the single control byte `ControlCodes.MetadataReceived` (`0x05`) on the same stream.
2. **Unidirectional data stream** — file body. Its `stream.Id` is included in the metadata so the receiver can correlate body to header (`ConnectionContext.GetFileMetadata(streamId)` on the server).
3. **Probe stream** — `PeerClient.ProbeConnection` opens an empty bidirectional stream right after `ConnectAsync` to force the TLS handshake to complete before `SendAsync`/`SendFileAsync` returns to the caller.

`ControlCodes` (`Common/ControlCodes.cs`) currently defines two bytes: `MetadataReceived = 0x05` and `ClientDisconnected = 0xA1` (passed to `QuicConnection.CloseAsync`). Adding new control codes means picking a value not in `ControlCodes` and handling it on both client and server.

### Certificates

Self-signed cert per peer, persisted as **two files** at the paths in `CertificateOptions`:

- `peer.crt` — PEM certificate.
- `key/peer.key` — PEM-encoded **encrypted** private key (passphrase chosen at first run).

`UnlockCommand` derives the runtime PFX in memory by calling `X509Certificate2.CreateFromEncryptedPem(...).Export(Pfx, password)`. `ServerBase` currently calls `BootstrapServer` with that cert but `AddClientAuthentication` is **commented out** in `GetConnectionOptionsAsync` (`Server/ServerBase.cs`) — so `Server:RequireClientCertificate` and the `IPeersStore` allow-list are wired up but not enforced. Re-enabling client auth means uncommenting that call and verifying the trust callback against the existing tests in `QuicPeer.Tests/Server/`.

### Logging

`Logging/LoggingExtensions.cs` configures Serilog with **two sinks**, split by `SourceContext` namespace:

- `logs/server.log` — anything from `QuicPeer.Server.*` (JSON formatted).
- `logs/system.log` — everything else (plain text).

Both have a 2 MB rolling cap. `Microsoft.*` is clamped to Warning. When debugging server behavior, tail `logs/server.log`; the console-side narrative lives in `logs/system.log`.
