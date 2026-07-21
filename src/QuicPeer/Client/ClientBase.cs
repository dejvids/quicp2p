using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using QuicPeer.Common;
using QuicPeer.Options;

namespace QuicPeer.Client;

public abstract class ClientBase
{
    private readonly X509Certificate2 _certificate;
    protected ClientOptions Options { get; }
    protected abstract Task RunClientInternal(QuicClientConnectionOptions options);

    protected ClientBase(IOptions<ClientOptions> options, X509Certificate2 certificate)
    {
        Options = options.Value;
        _certificate = certificate;
    }

    public async Task RunClientAsync()
    {
        var options = BootstrapClient();
        await RunClientInternal(options);
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
                ClientCertificates = [_certificate]
            },
            MaxInboundBidirectionalStreams = Options.MaxInboundBidirectionalStreams,
            MaxInboundUnidirectionalStreams = Options.MaxInboundUnidirectionalStreams,
            KeepAliveInterval = TimeSpan.FromSeconds(Options.KeepAliveInterval)
        };

        return options;
    }

    private bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain,
        SslPolicyErrors sslPolicyErrors) =>
        certificate is not null &&
        !new Certificate(new X509Certificate2(certificate)).IsExpired(TimeProvider.System) &&
        (!Options.ValidateFullChain || sslPolicyErrors == SslPolicyErrors.None);
}