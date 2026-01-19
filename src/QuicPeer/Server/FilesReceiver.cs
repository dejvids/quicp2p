using System.Collections.Concurrent;
using System.Net.Quic;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Options;

namespace QuicPeer.Server;

public class FilesReceiver : IFilesReceiver
{
    private readonly FilesReceiverOptions _options;
    private readonly IChecksumProvider _checksumProvider;
    private readonly ConcurrentDictionary<long, FileMetadata> _files = new();
    private readonly ILogger _logger;

    public FilesReceiver(IOptions<FilesReceiverOptions> options, IChecksumProvider checksumProvider,
        ILogger<FilesReceiver> logger)
    {
        _options = options.Value;
        _checksumProvider = checksumProvider;
        _logger = logger;
    }

    public void AcceptFile(FileMetadata metadata)
    {
        _files.AddOrUpdate(metadata.DataStreamId, metadata, (_, _) => metadata);
    }

    public async Task ReceiveFileAsync(QuicStream stream, CancellationToken ct)
    {
        var metadata = _files.GetValueOrDefault(stream.Id);
        if (metadata is null)
        {
            return;
        }

        FileInfo downloadFileInfo;
        try
        {
            downloadFileInfo = await CopyToFile(stream, ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while downloading a file");
            return;
        }

        try
        {
            _checksumProvider.VerifyChecksum(downloadFileInfo, metadata.Checksum);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while verifying a checksum of downloaded file.");
            return;
        }

        try
        {
            RenameFile(metadata.FileName, downloadFileInfo);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while renaming a file.");
        }
    }

    private void RenameFile(string originalFilename, FileInfo downloadFileInfo)
    {
        var destinationPath = Path.Combine(_options.DownloadsDirectory, originalFilename);

        if (File.Exists(destinationPath))
        {
            destinationPath = GetUniqueFilenameForCopy(destinationPath);
        }

        downloadFileInfo.MoveTo(destinationPath, false);
    }

    private static string GetUniqueFilenameForCopy(string destinationPath)
    {
        var destinationFileName = Path.GetFileNameWithoutExtension(destinationPath);
        var destinationFileExtension = Path.GetExtension(destinationPath);
        var directoryPath = Path.GetDirectoryName(destinationPath);
        if (directoryPath is null)
        {
            return destinationPath;
        }

        var copyIndex = 1;
        do
        {
            destinationPath = Path.Combine(directoryPath,
                $"{destinationFileName}({copyIndex++}){destinationFileExtension}");
        } while (File.Exists(destinationPath));

        return destinationPath;
    }

    private async Task<FileInfo> CopyToFile(QuicStream sourceStream, CancellationToken ct)
    {
        var filePath = Path.Combine(_options.DownloadsDirectory, Path.GetRandomFileName());
        var fileInfo = new FileInfo(filePath);
        var fileStream = fileInfo.Create();
        try
        {
            Directory.CreateDirectory(_options.DownloadsDirectory);
            await sourceStream.CopyToAsync(fileStream, ct);
            await fileStream.FlushAsync(ct);
        }
        finally
        {
            await fileStream.DisposeAsync();
            await sourceStream.DisposeAsync();
        }

        return fileInfo;
    }
}