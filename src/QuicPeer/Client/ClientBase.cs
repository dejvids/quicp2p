using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Options;

namespace QuicPeer.Client;

public abstract class ClientBase(IOptions<ClientOptions> options)
{
    protected ClientOptions Options => options.Value;
    protected abstract Task RunClientInternal(QuicClientConnectionOptions options, CancellationToken ct);
    public async Task RunClientAsync(CancellationToken ct)
    {

        var options = BootstrapClient();
        options.ClientAuthenticationOptions.LocalCertificateSelectionCallback = LoadClientCertificate;

        await RunClientInternal(options, ct);
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
