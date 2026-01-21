using System.IO.Abstractions;
using NSubstitute;
using QuicPeer.Common;
using QuicPeer.Common.Exceptions;

namespace QuicPeer.Tests.Common;

public class ChecksumProviderTests
{
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    
    [Fact]
    public void should_throw_exception_if_file_not_exists()
    {
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(false);

        var exception = Record.Exception(() => new CheckSumProvider().GetChecksum(fileInfo));
        Assert.IsType<FileNotFoundException>(exception);
    }

    [Fact]
    public void should_calculate_SHA256_checksum_and_write_as_hex_string()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        var fileStreamMock = MockFileStream(data);

        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(true);
        fileInfo.OpenRead().Returns(fileStreamMock);
        
        var checksum = new CheckSumProvider().GetChecksum(fileInfo);
        
        Assert.NotNull(checksum);
        Assert.Equal(64, checksum.Length);
        Assert.Equal(32, Convert.FromHexString(checksum).Length);
    }

    [Fact]
    public void should_throw_exception_if_checksum_mismatch()
    {
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        var fileStreamMock = MockFileStream(data);

        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(true);
        fileInfo.OpenRead().Returns(fileStreamMock);
        
        var exception = Record.Exception(() => 
            new CheckSumProvider().VerifyChecksum(fileInfo, "INVALID_CHECKSUM"));
        
        Assert.IsType<DataIntegrityException>(exception);
    }

    [Fact]
    public void should_not_throw_any_exception_if_checksum_matches()
    {
        const string expectedChecksum = "ad95131bc0b799c0b1af477fb14fcf26a6a9f76079e48bf090acb7e8367bfd0e";
        
        byte[] data = [0xFF, 0xFF, 0xFF, 0xFF];
        var fileStreamMock = MockFileStream(data);

        var fileInfo = Substitute.For<IFileInfo>();
        fileInfo.Exists.Returns(true);
        fileInfo.OpenRead().Returns(fileStreamMock);
        
        var exception = Record.Exception(() => 
            new CheckSumProvider().VerifyChecksum(fileInfo, expectedChecksum));
        
        Assert.Null(exception);
    }

    private static FileSystemStream MockFileStream(byte[] data)
    {
        var dataStream = new MemoryStream(data);
        var fileStreamMock =  Substitute.For<FileSystemStream>(Substitute.For<Stream>(), "text.txt", false);
        fileStreamMock.Read(Arg.Any<byte[]>(), Arg.Any<int>(), Arg.Any<int>())
            .Returns(c => dataStream.Read(c.Arg<byte[]>(), c.ArgAt<int>(1), c.ArgAt<int>(2)));
        
        return fileStreamMock;
    }
}