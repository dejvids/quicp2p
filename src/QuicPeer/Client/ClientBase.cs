using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Common.ValueObjects;
using QuicPeer.Options;

namespace QuicPeer.Client;

public abstract class ClientBase
{
    protected ClientOptions Options { get; }
    protected abstract Task RunClientInternal(QuicClientConnectionOptions options, CancellationToken ct);

    protected ClientBase(IOptions<ClientOptions> options)
    {
        Options = options.Value;
    }

    public async Task RunClientAsync(CancellationToken ct)
    {
        var options = BootstrapClient();
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
            ClientAuthenticationOptions = new SslClientAuthenticationOptions
            {
                ApplicationProtocols = [Options.ClientCertificate.ApplicationProtocol],
                RemoteCertificateValidationCallback = ValidateServerCertificate,
                LocalCertificateSelectionCallback = LoadClientCertificate,
            },
            MaxInboundBidirectionalStreams = Options.MaxInboundBidirectionalStreams,
            MaxInboundUnidirectionalStreams = Options.MaxInboundUnidirectionalStreams,
            KeepAliveInterval = TimeSpan.FromSeconds(Options.KeepAliveInterval)
        };

        return options;
    }

    private X509Certificate2 LoadClientCertificate(object sender, string targetHost,
        X509CertificateCollection localCertificates, X509Certificate? remoteCertificate, string[] acceptableIssuers)
    {
        var certPath = Options.ClientCertificate.Path;
        return X509CertificateLoader.LoadPkcs12FromFile(certPath, string.Empty);
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain,
        SslPolicyErrors sslPolicyErrors) =>
        certificate is not null &&
        new Certificate(certificate).IsNotExpired() &&
        (!Options.ValidateFullChain || sslPolicyErrors == SslPolicyErrors.None);
}