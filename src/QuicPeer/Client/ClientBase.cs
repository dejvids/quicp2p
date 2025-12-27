using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;

namespace QuicPeer.Client;

[SupportedOSPlatform("windows")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public abstract class ClientBase
{
    public async Task RunClientAsync()
    {
        try
        {
            var options = BootstrapClient();

            await Task.Delay(1000);
            await RunClientInternal(options);
        }
        catch (NotSupportedException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    private static QuicClientConnectionOptions BootstrapClient()
    {
        if (!QuicConnection.IsSupported)
        {
            throw new NotSupportedException("QuicConnection is not supported");
        }

        QuicClientConnectionOptions options = new()
        {
            DefaultStreamErrorCode = 0xA,
            DefaultCloseErrorCode = 0xB,
            ClientAuthenticationOptions = new()
            {
                ApplicationProtocols = [new SslApplicationProtocol("quic-peer")],
                RemoteCertificateValidationCallback = ValidateServerCertificate
            },
            MaxInboundBidirectionalStreams = 10,
            MaxInboundUnidirectionalStreams = 10
        };

        return options;
    }

    protected abstract Task RunClientInternal(QuicClientConnectionOptions options);

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
