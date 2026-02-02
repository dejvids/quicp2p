using System.Diagnostics;
using System.IO.Abstractions;
using System.Net;
using System.Net.Quic;
using System.Text;
using Microsoft.Extensions.Options;
using QuicPeer.Client.Exceptions;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Options;

namespace QuicPeer.Client;

public sealed class PeerClient(
    ILogger<PeerClient> logger,
    IOptions<ClientOptions> options,
    IPEndPoint remoteEndpoint,
    IChecksumProvider checksumProvider)
    : ClientBase(options), IPeerClient
{
    private QuicConnection? _connection;
    private CancellationTokenSource _cts = new();
    private readonly Stopwatch _stopwatch = new();

    public EndPoint? RemoteEndpoint { get; private set; }

    protected override async Task RunClientInternal(QuicClientConnectionOptions options, CancellationToken ct)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        options.RemoteEndPoint = remoteEndpoint;
        options.ClientAuthenticationOptions.TargetHost = remoteEndpoint.Address.ToString();

        var connection = await QuicConnection.ConnectAsync(options, ct);
        _connection = connection;
        RemoteEndpoint = connection.RemoteEndPoint;
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

    public async Task SendFileAsync(IFileInfo file)
    {
        if (_connection is null || !file.Exists)
        {
            return;
        }

        var dataStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Unidirectional, _cts.Token);
        logger.LogDebug("Opened stream: {StreamType} with ID: {StreamId}" , dataStream.Type, dataStream.Id);
        var metadataStream = await _connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional, _cts.Token);
        logger.LogDebug("Opened stream: {StreamType} with ID: {StreamId}" , metadataStream.Type, metadataStream.Id);
        var checksum = checksumProvider.GetChecksum(file);
        var metadata = new FileMetadata(file.Name, file.Length, checksum, dataStream.Id);

        try
        {
            await SendMetadata(metadata, metadataStream);

            await using var fileStream = file.OpenRead();
            _stopwatch.Start();
            await fileStream.CopyToAsync(dataStream, Options.Transfer.BufferSize, _cts.Token);
            _stopwatch.Stop();
            dataStream.CompleteWrites();
            dataStream.Close();
        }
        finally
        {
            _stopwatch.Stop();
            await dataStream.DisposeAsync();
            await metadataStream.DisposeAsync();
            logger.LogInformation("Upload time: {Time}", _stopwatch.Elapsed);
            logger.LogInformation("Stream {Id} closed", dataStream.Id);
            logger.LogInformation("Stream {Id} closed", metadataStream.Id);
        }
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
        await _cts.CancelAsync();
        if (_connection is not null)
        {
            await _connection.CloseAsync(ControlCodes.ClientDisconnected);
            await _connection.DisposeAsync();
        }
    }

    public ValueTask DisposeAsync()
    {
        _cts.Dispose();

        return ValueTask.CompletedTask;
    }
}