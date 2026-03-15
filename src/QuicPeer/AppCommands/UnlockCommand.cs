using System.IO.Abstractions;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Options;
using QuicPeer.Client.Abstraction;
using QuicPeer.Common;
using QuicPeer.Common.Messaging;
using QuicPeer.Common.Messaging.ClientQueue;
using QuicPeer.Options;
using Spectre.Console;

namespace QuicPeer.AppCommands;

public class UnlockCommand(
    ILogger<UnlockCommand> logger,
    IConsoleAccessor consoleAccessor,
    IMessageQueue<IClientMessage> messageQueue,
    IOptions<CertificateOptions> certificateOptions,
    IPeerClientFactory peerClientFactory,
    IFileSystem fileSystem)
    : AppCommand(logger, consoleAccessor)
{
    private readonly IMessageQueue<IClientMessage> _messageQueue = messageQueue;
    private readonly CertificateOptions _certificateOptions = certificateOptions.Value;
    private readonly IPeerClientFactory _peerClientFactory = peerClientFactory;
    private readonly IFileSystem _fileSystem = fileSystem;

    public override string CommandName => "unlock";
    public override async ValueTask<CommandResult> Execute(CancellationToken cancellationToken)
    {
        if (CertificateNotFound())
        {
            await CreateCertificate(cancellationToken);
        }
        var certificate = await LoadCertificate(cancellationToken);
        Console.Clear();
        if (certificate is null)
        {
            return CommandResult.Fail;
        }
        
        await _messageQueue.EnqueueAsync(new Unlocked(certificate.RawData));
        _peerClientFactory.SetCertificate(certificate);
        return CommandResult.Success;
    }

    private bool CertificateNotFound()
    {
        return !_fileSystem.File.Exists(_certificateOptions.CertPath) ||
               !_fileSystem.File.Exists(_certificateOptions.KeyPath);
    }

    private async Task CreateCertificate(CancellationToken ct)
    {
        Console.Write("Creating new certificate.");
        var passwordPrompt = ConsoleAccessor.PasswordPrompt("Password:");
        
        var passphrase = await passwordPrompt.ShowAsync(Console, ct);
        
        await CreateSelfSignedCertificate(passphrase);
    }

    private async Task<X509Certificate2?> LoadCertificate(CancellationToken cancellationToken)
    {
        var passwordPrompt = ConsoleAccessor.PasswordPrompt("Password:");
        var retry = true;

        while(retry)
        {
            try
            {
                var password = await passwordPrompt.ShowAsync(Console, cancellationToken);
                var certPem = await _fileSystem.File.ReadAllTextAsync(_certificateOptions.CertPath, cancellationToken);
                var keyPem = await _fileSystem.File.ReadAllTextAsync(_certificateOptions.KeyPath, cancellationToken);

                return X509Certificate2.CreateFromEncryptedPem(certPem, keyPem, password);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Failed to load certificate own certificate");
                Console.MarkupLine("[red]Incorrect password.[/]");
                retry = await Console.ConfirmAsync("Try again.", cancellationToken: cancellationToken);
                Console.Clear();
            }
        }
        
        return null;
    }
    
    private async Task CreateSelfSignedCertificate(string passphrase)
    {
        var certificate = new Certificate(_certificateOptions, TimeProvider.System);
    
        var crt = certificate.GetPem();
        var privateKey = certificate.GetPrivateKey(passphrase);
        var keyDirectory = _fileSystem.Path.GetDirectoryName(_certificateOptions.KeyPath);
        if (!string.IsNullOrEmpty(keyDirectory))
        {
            _fileSystem.Directory.CreateDirectory(keyDirectory);
        }
        
        await _fileSystem.File.WriteAllTextAsync(_certificateOptions.KeyPath, privateKey, Encoding.UTF8);
        await _fileSystem.File.WriteAllTextAsync(_certificateOptions.CertPath, crt, Encoding.UTF8);
    }
}