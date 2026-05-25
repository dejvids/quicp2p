using System.Diagnostics;
using System.IO.Abstractions;
using System.Net;
using System.Net.Quic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Options;
using QuicPeer.Client.Abstraction;
using QuicPeer.Client.Exceptions;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Options;

namespace QuicPeer.Client;

public sealed class PeerClient : ClientBase, IPeerClient
{
    private readonly IPEndPoint _remoteEndpoint;
    private readonly IChecksumProvider _checksumProvider;
    private readonly CancellationTokenSource _cts;
    private QuicConnection? _connection;
    private bool _disposed;

    public PeerClient(
        IOptions<ClientOptions> options,
        IPEndPoint remoteEndpoint,
        X509Certificate2 certificate,
        IChecksumProvider checksumProvider,
        CancellationTokenSource cts)
        : base(options, certificate)
    {
        _remoteEndpoint = remoteEndpoint;
        _checksumProvider = checksumProvider;
        _cts = cts;
    }

    public EndPoint? RemoteEndpoint { get; private set; }

    protected override async Task RunClientInternal(QuicClientConnectionOptions options)
    {
        options.RemoteEndPoint = _remoteEndpoint;
        options.ClientAuthenticationOptions.TargetHost = _remoteEndpoint.Address.ToString();

        var connection = await QuicConnection.ConnectAsync(options, _cts.Token);
        await ProbeConnection(connection, _cts.Token);
        _connection = connection;
        RemoteEndpoint = _connection.RemoteEndPoint;
    }

    public async Task SendAsync(string message)
    {
        if (_connection is null)
        {
            return;
        }

        var textStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, _cts.Token);
        textStream.WriteTimeout = 100;

        var payload = Encoding.UTF8.GetBytes(message);
        await textStream.WriteAsync(payload);
        await textStream.FlushAsync(_cts.Token);
        textStream.CompleteWrites();
    }

    public async Task<SendFileResult> SendFileAsync(IFileInfo file)
    {
        if (_connection is null || !file.Exists)
        {
            return SendFileResult.Empty;
        }

        var dataStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional, _cts.Token);
        var metadataStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, _cts.Token);
        var checksum = _checksumProvider.GetChecksum(file);
        var metadata = new FileMetadata(file.Name, file.Length, checksum, dataStream.Id);

        var stopwatch = new Stopwatch();
        try
        {
            await SendMetadata(metadata, metadataStream);

            await using var fileStream = file.OpenRead();
            stopwatch.Start();
            await fileStream.CopyToAsync(dataStream, Options.Transfer.BufferSize, _cts.Token);
            stopwatch.Stop();
            dataStream.CompleteWrites();
            dataStream.Close();
        }
        finally
        {
            stopwatch.Stop();
            await dataStream.DisposeAsync();
            await metadataStream.DisposeAsync();
        }

        return new SendFileResult(stopwatch.Elapsed);
    }

    private async Task SendMetadata(FileMetadata metadata, QuicStream metadataStream)
    {
        const int timeout = 3000;
        var jsonPayload = System.Text.Json.JsonSerializer.Serialize(metadata);
        var payload = Encoding.UTF8.GetBytes(jsonPayload);
        await metadataStream.WriteAsync(payload);
        await metadataStream.FlushAsync(_cts.Token);

        var acknowledgement = new byte[1];
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
        cts.CancelAfter(timeout);
        await metadataStream.ReadExactlyAsync(acknowledgement, 0, acknowledgement.Length, cts.Token);

        var code = acknowledgement[0];

        if (code != ControlCodes.MetadataReceived)
        {
            throw new MetadataException($"Did not receive metadata acknowledgement. Code: {code:X}");
        }

        metadataStream.CompleteWrites();
    }

    public async Task DisconnectAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _cts.CancelAsync();
        if (_connection is not null)
        {
            await _connection.CloseAsync(ControlCodes.ClientDisconnected);
        }
    }

    private async Task ProbeConnection(QuicConnection connection, CancellationToken ct)
    {
        await Task.Delay(Options.TlsHandshakeDelay, ct);
        await using var probeStream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, ct);
        await probeStream.WriteAsync(Memory<byte>.Empty, ct);
        probeStream.CompleteWrites();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        try
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }
        }
        finally
        {
            _cts.Dispose();
        }
    }
}