using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Client;

public abstract class ClientBase(ILogger logger, IOptions<ClientOptions> options)
{
    protected ILogger Logger { get; } = logger;
    protected ClientOptions Options => options.Value;
    protected abstract Task RunClientInternal(QuicClientConnectionOptions options, CancellationToken ct);
    public async Task RunClientAsync(CancellationToken ct)
    {
        try
        {
            var options = BootstrapClient();
            options.ClientAuthenticationOptions.LocalCertificateSelectionCallback = LoadClientCertificate;


            await Task.Delay(1000, ct);
            await RunClientInternal(options, ct);
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private QuicClientConnectionOptions BootstrapClient()
    {
        if (!QuicConnection.IsSupported)
        {
            throw new NotSupportedException("QuicConnection is not supported");
        }

        QuicClientConnectionOptions options = new()
        {
            DefaultStreamErrorCode = Options.DefaultStreamErrorCode,
            DefaultCloseErrorCode = Options.DefaultCloseErrorCode,
            ClientAuthenticationOptions = new()
            {
                ApplicationProtocols = [Options.ClientCertificate.ApplicationProtocol],
                RemoteCertificateValidationCallback = ValidateServerCertificate
            },
            MaxInboundBidirectionalStreams = Options.MaxInboundBidirectionalStreams,
            MaxInboundUnidirectionalStreams = Options.MaxInboundUnidirectionalStreams,
            KeepAliveInterval = TimeSpan.FromSeconds(3)
        };

        return options;
    }


    private X509Certificate2? LoadClientCertificate(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate? remoteCertificate, string[] acceptableIssuers)
    {
        string certPath = Options.ClientCertificate.Path;
        return X509CertificateLoader.LoadPkcs12FromFile(certPath, string.Empty);
    }

    static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            Console.WriteLine("Certificate validation successful.");
            return true;
        }

        Console.WriteLine(sslPolicyErrors);

        return true; // For poc purposes, accept the certificate anyway.
    }
}
