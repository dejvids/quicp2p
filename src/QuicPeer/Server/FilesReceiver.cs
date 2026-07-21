using System.Buffers;
using System.IO.Abstractions;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Options;

namespace QuicPeer.Server;

public class FilesReceiver : IFilesReceiver
{
    private readonly ServerTransferOptions _options;
    private readonly IChecksumProvider _checksumProvider;
    private readonly ILogger _logger;
    private readonly IFileSystem _fileSystem;

    public FilesReceiver(IOptions<ServerOptions> options, IChecksumProvider checksumProvider,
        ILogger<FilesReceiver> logger, IFileSystem fileSystem)
    {
        _options = options.Value.Transfer;
        _checksumProvider = checksumProvider;
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task ReceiveFileAsync(Stream stream, FileMetadata metadata, CancellationToken ct)
    {
        DownloadedFile downloadedFile;
        try
        {
            downloadedFile = await CopyToFile(stream, ct);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while downloading a file");
            return;
        }

        try
        {
            _checksumProvider.VerifyChecksum(downloadedFile.Checksum, metadata.Checksum);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while verifying a checksum of downloaded file.");
            RenameToInvalid(downloadedFile.File);
            return;
        }

        try
        {
            RenameFile(metadata.FileName, downloadedFile.File);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while renaming a file.");
        }
    }

    private void RenameToInvalid(IFileInfo downloadFileInfo)
    {
        var newFileName = _fileSystem.Path.ChangeExtension(downloadFileInfo.Name, "invalid");

        downloadFileInfo.MoveTo(newFileName, true);
    }

    private void RenameFile(string originalFilename, IFileInfo downloadFileInfo)
    {
        var destinationPath = _fileSystem.Path.Combine(_options.DownloadsDirectory, originalFilename);

        if (_fileSystem.File.Exists(destinationPath))
        {
            destinationPath = GetUniqueFilenameForCopy(destinationPath);
        }

        downloadFileInfo.MoveTo(destinationPath, false);
    }

    private string GetUniqueFilenameForCopy(string destinationPath)
    {
        var destinationFileName = _fileSystem.Path.GetFileNameWithoutExtension(destinationPath);
        var destinationFileExtension = _fileSystem.Path.GetExtension(destinationPath);
        var directoryPath = _fileSystem.Path.GetDirectoryName(destinationPath);
        if (directoryPath is null)
        {
            return destinationPath;
        }

        var copyIndex = 1;
        do
        {
            destinationPath = _fileSystem.Path.Combine(directoryPath,
                $"{destinationFileName}({copyIndex++}){destinationFileExtension}");
        } while (_fileSystem.File.Exists(destinationPath));

        return destinationPath;
    }

    private async Task<DownloadedFile> CopyToFile(Stream sourceStream, CancellationToken ct)
    {
        var filePath = _fileSystem.Path.Combine(_options.DownloadsDirectory, _fileSystem.Path.GetRandomFileName());
        filePath = _fileSystem.Path.ChangeExtension(filePath, "download");
        var fileInfo = _fileSystem.FileInfo.New(filePath);
        _fileSystem.Directory.CreateDirectory(_options.DownloadsDirectory);
        await using var fileStream = fileInfo.Create();
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(_options.BufferSize);
        try
        {
            int read;
            while ((read = await sourceStream.ReadAsync(buffer.AsMemory(0, _options.BufferSize), ct)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            }

            await fileStream.FlushAsync(ct);
            return new DownloadedFile(fileInfo, Convert.ToHexString(hash.GetHashAndReset()));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            await sourceStream.DisposeAsync();
        }
    }

    private sealed record DownloadedFile(IFileInfo File, string Checksum);
}
