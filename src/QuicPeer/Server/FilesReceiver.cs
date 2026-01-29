using System.IO.Abstractions;
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
        IFileInfo downloadFileInfo;
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
            RenameToInvalid(downloadFileInfo);
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

    private void RenameToInvalid(IFileInfo downloadFileInfo)
    {
        _fileSystem.Path.ChangeExtension(downloadFileInfo.Name, "invalid");
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

    private async Task<IFileInfo> CopyToFile(Stream sourceStream, CancellationToken ct)
    {
        var filePath = _fileSystem.Path.Combine(_options.DownloadsDirectory, _fileSystem.Path.GetRandomFileName());
        var fileInfo = _fileSystem.FileInfo.New(filePath);
        _fileSystem.Directory.CreateDirectory(_options.DownloadsDirectory);
        var fileStream = fileInfo.Create();
        try
        {
            await sourceStream.CopyToAsync(fileStream, _options.BufferSize, ct);
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