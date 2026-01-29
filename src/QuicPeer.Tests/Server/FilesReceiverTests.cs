using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.Common;
using QuicPeer.Common.Dto;
using QuicPeer.Common.Exceptions;
using QuicPeer.Options;
using QuicPeer.Server;

namespace QuicPeer.Tests.Server;

public class FilesReceiverTests
{
    private readonly ILogger<FilesReceiver> _logger = Substitute.For<ILogger<FilesReceiver>>();
    private readonly IOptions<ServerOptions> _options = Substitute.For<IOptions<ServerOptions>>();
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();

    public FilesReceiverTests()
    {
        _options.Value.Returns(new ServerOptions(){ServerCertificate = new()});
    }

    private static IFileInfo MockFileSystem(IFileSystem fileSystem, string filename)
    {
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Name.Returns(filename);
        fileInfo.Create().Returns(Substitute.For<FileSystemStream>(Substitute.For<Stream>(), filename, false));
        fileSystem.FileInfo.New(Arg.Any<string>()).Returns(fileInfo);
        fileSystem.Path.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(c => c.ArgAt<string>(1));

        return fileInfo;
    }

    private static FileMetadata DefaultFileMetadata() =>
        new FileMetadata(FileName: "test.txt", Checksum: "checksum", DataStreamId: 1, FileSize: 1024);

    [Fact]
    public async Task should_create_directory_for_downloading_files()
    {
        const string downloadDirName = "downloadDir";
        _options.Value.Transfer =new ServerTransferOptions() { DownloadsDirectory = downloadDirName};
        var fileReceiver = new FilesReceiver(_options, Substitute.For<IChecksumProvider>(), _logger,
            _fileSystem);
        var stream = Substitute.For<Stream>();
        var metadata = DefaultFileMetadata();

        await fileReceiver.ReceiveFileAsync(stream, metadata, CancellationToken.None);

        _fileSystem.Directory.Received(1).CreateDirectory(Arg.Is<string>(x => x == downloadDirName));
    }

    [Fact]
    public async Task should_verify_checksum()
    {
        const string filename = "filename";
        _ = MockFileSystem(_fileSystem, filename);
        var  checksumProvider = Substitute.For<IChecksumProvider>();
        var metadata = DefaultFileMetadata();
        var filesReceiver = new FilesReceiver(_options, checksumProvider, _logger,
            _fileSystem);
        
        await filesReceiver.ReceiveFileAsync(Substitute.For<Stream>(), metadata, CancellationToken.None);
        
        checksumProvider.Received(1).VerifyChecksum(Arg.Any<IFileInfo>(), Arg.Is<string>(x => x == metadata.Checksum));
    }

    [Fact]
    public async Task should_set_specific_name_if_checksum_does_not_match()
    {
        const string filename = "test.txt";
        var checksumProvider = Substitute.For<IChecksumProvider>();
        checksumProvider.When(x => x.VerifyChecksum(Arg.Any<IFileInfo>(), Arg.Any<string>()))
            .Do(_ => throw new DataIntegrityException("Checksum mismatch"));
        var metadata = DefaultFileMetadata();
        _ = MockFileSystem(_fileSystem, filename);

        var fileReceiver = new FilesReceiver(_options, checksumProvider, Substitute.For<ILogger<FilesReceiver>>(),
            _fileSystem);

        await fileReceiver.ReceiveFileAsync(Substitute.For<Stream>(), metadata, CancellationToken.None);

        _fileSystem.Path.Received(1).ChangeExtension(Arg.Is<string>(src => src == filename),
            Arg.Is<string>(dest => dest.Contains("invalid", StringComparison.InvariantCultureIgnoreCase)));
    }

    [Fact]
    public async Task should_rename_file_to_original_name()
    {
        const string filename = "test.txt";
        var fileInfo = MockFileSystem(_fileSystem, filename);
        var  checksumProvider = Substitute.For<IChecksumProvider>();
        var metadata = DefaultFileMetadata();
        
        var filesReceiver = new FilesReceiver(_options, checksumProvider, Substitute.For<ILogger<FilesReceiver>>(),
            _fileSystem);
        
        await filesReceiver.ReceiveFileAsync(Substitute.For<Stream>(), metadata, CancellationToken.None);
        
        fileInfo.Received(1).MoveTo(Arg.Is<string>(dest => dest.Contains(metadata.FileName)), Arg.Any<bool>());
    }

    [Fact]
    public async Task should_copy_from_source_to_target_stream()
    {
        const string filename = "test.txt";
        _ = MockFileSystem(_fileSystem, filename);
        var  checksumProvider = Substitute.For<IChecksumProvider>();
        var metadata = DefaultFileMetadata();
        var sourceStream = Substitute.For<Stream>();
        var filesReceiver = new FilesReceiver(_options, checksumProvider, Substitute.For<ILogger<FilesReceiver>>(),
            _fileSystem);
        
        await filesReceiver.ReceiveFileAsync(sourceStream, metadata, CancellationToken.None);
        
        await sourceStream.Received(1).CopyToAsync(Arg.Any<FileSystemStream>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task should_get_next_copy_name()
    {
        const string filename = "temp.download";
        const string originalFilename = "test.txt";
        var fileInfo = MockFileSystem(_fileSystem, filename);
        var  checksumProvider = Substitute.For<IChecksumProvider>();
        var metadata = DefaultFileMetadata();
        metadata = metadata with {FileName = originalFilename};
        var fileReceiver = new FilesReceiver(_options, checksumProvider, Substitute.For<ILogger<FilesReceiver>>(),
            _fileSystem);
        _fileSystem.File.Exists(Arg.Is<string>(p => p.Contains(originalFilename)))
            .Returns(true);

        await fileReceiver.ReceiveFileAsync(Substitute.For<Stream>(), metadata, CancellationToken.None);
        
        fileInfo.Received(1)
            .MoveTo(Arg.Is<string>(dest => dest.Contains("(1)")), Arg.Any<bool>());
    }
}