using System.IO.Abstractions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using QuicPeer.AppCommands;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Options;
using Spectre.Console;

namespace QuicPeer.Tests.AppCommands;

public class UnlockCommandTests : AppCommandTestsBase
{
    private const string CertContent = """
                                       -----BEGIN CERTIFICATE-----
                                       MIIBKDCBz6ADAgECAghySRPSujCJzzAKBggqhkjOPQQDAjASMRAwDgYDVQQDEwdQ
                                       ZWVyNTAxMB4XDTI2MDMwNDIxMTA1OFoXDTM2MDMwMjIxMTA1OFowEjEQMA4GA1UE
                                       AxMHUGVlcjUwMTBZMBMGByqGSM49AgEGCCqGSM49AwEHA0IABBLVPXPjVg1FZ4F0
                                       gRHppcNpoPQFtDZT2RudYqwZBDS0hH69yGcAlmOBofHnFZVOieH4ZD3TOKro6bXI
                                       UyiizkyjDzANMAsGA1UdDwQEAwIEsDAKBggqhkjOPQQDAgNIADBFAiBKSdYTau5b
                                       ouDp/VoR33FObvVIZZPW4XgZq/iHcFtFdgIhAO3VPlPINSz/s1+LDrB8WJnohas5
                                       TXBNurVucKHNfID3
                                       -----END CERTIFICATE-----
                                       """;
    
    private const string KeyContent = """
                                      -----BEGIN ENCRYPTED PRIVATE KEY-----
                                      MIIBEzBeBgkqhkiG9w0BBQ0wUTAwBgkqhkiG9w0BBQwwIwQQxTy3G08Z50l46VB8
                                      MltKfgIBAzAMBggqhkiG9w0CCQUAMB0GCWCGSAFlAwQBAgQQ6yalWyI0p63NDZZw
                                      6lYC+QSBsAfI7WNnINox5jK0pr5mdHPEkeKbl1Z4GyNKdXoMxZgrIIieKUizoHXF
                                      M+/ZKoFhul7I5HuFyemkDT5ApMuxQmEh07mblHRTLqfSBZ8Bnv5k1PDDRCQsSw49
                                      SfhWLyq8RK/tVOP0l+qAI2muQgu11vMEmDh7beqvxYNSqOqvSoO4NDZNBwzls3HG
                                      vexWXH33+8evlj++eSc+x3N5/MFpg0okr4RFSYe4vYEbESKgc7Vz
                                      -----END ENCRYPTED PRIVATE KEY-----
                                      """;

    private const string Password = "1234";
    
    private const string KeyPath = "test.key";
    private const string CertPath = "test.crt";
    private readonly IFileSystem _fileSystem = Substitute.For<IFileSystem>();
    private readonly ILogger<UnlockCommand> _logger = Substitute.For<ILogger<UnlockCommand>>();
    private readonly IOptions<CertificateOptions> _certificateOptions = Substitute.For<IOptions<CertificateOptions>>();

    public UnlockCommandTests()
    {
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(true);
        _fileSystem.File.ReadAllTextAsync(Arg.Is<string>(path => path == CertPath), Arg.Any<CancellationToken>())
            .Returns(CertContent);
        _fileSystem.File.ReadAllTextAsync(Arg.Is<string>(path => path == KeyPath), Arg.Any<CancellationToken>())
            .Returns(KeyContent);

        _certificateOptions.Value.Returns(new CertificateOptions
        {
            CertPath = CertPath,
            KeyPath = KeyPath
        });

        var passwordPrompt = Substitute.For<IPrompt<string>>();
        passwordPrompt.ShowAsync(Arg.Any<IAnsiConsole>(), Arg.Any<CancellationToken>())
            .Returns(Password);
        ConsoleAccessor.PasswordPrompt(Arg.Any<string>()).Returns(passwordPrompt);
    }

    [Fact]
    public async Task clients_factory_should_set_certificate()
    {
        var clientsFactory = Substitute.For<IPeerClientFactory>();

        var unlockCommand = new UnlockCommand(_logger, 
            ConsoleAccessor,
            Substitute.For<IMessageQueue<IClientMessage>>(), 
            _certificateOptions, 
            clientsFactory,
            _fileSystem);

        await unlockCommand.Execute(CancellationToken);
        
        clientsFactory.Received(1).SetCertificate(Arg.Any<X509Certificate2>());
    }
    
    [Fact]
    public async Task should_send_unlocked_message()
    {
        var messageQueue = Substitute.For<IMessageQueue<IClientMessage>>();
        var unlockCommand = new UnlockCommand(_logger, 
            ConsoleAccessor,
            messageQueue, 
            _certificateOptions, 
            Substitute.For<IPeerClientFactory>(),
            _fileSystem);

        await unlockCommand.Execute(CancellationToken);
        
        await messageQueue.Received(1).EnqueueAsync(Arg.Any<Unlocked>());
    }

    [Fact]
    public async Task should_create_self_signed_if_not_exists()
    {
        _fileSystem.File.Exists(Arg.Any<string>()).Returns(false);
        
        var unlockCommand = new UnlockCommand(_logger, 
            ConsoleAccessor,
            Substitute.For<IMessageQueue<IClientMessage>>(), 
            _certificateOptions, 
            Substitute.For<IPeerClientFactory>(),
            _fileSystem);

        await unlockCommand.Execute(CancellationToken);

        await _fileSystem.File.Received(1).WriteAllTextAsync(Arg.Is<string>(path => path == CertPath),
            Arg.Any<string>(),
            Arg.Is<Encoding>(encoding => encoding == Encoding.UTF8),
            Arg.Any<CancellationToken>());
        
        await _fileSystem.File.Received(1).WriteAllTextAsync(Arg.Is<string>(path => path == KeyPath),
            Arg.Any<string>(),
            Arg.Is<Encoding>(encoding => encoding == Encoding.UTF8),
            Arg.Any<CancellationToken>());
    }
}