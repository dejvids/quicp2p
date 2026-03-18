using System.Globalization;
using System.Security.Cryptography.X509Certificates;
using NSubstitute;
using QuicPeer.Options;

namespace QuicPeer.Tests.Common;

public class CertificateTests
{
    private const string TestCertificate =
        """
        Certificate:
        Data:
            Version: 3 (0x2)
            Serial Number:
                0b:2d:82:21:b4:32:15:7b:60:0f:b1:41:7e:86:3d:b9:49:14:6c:f1
            Signature Algorithm: ecdsa-with-SHA256
            Issuer: CN = quicp2p.test
            Validity
                Not Before: Feb 10 21:41:34 2026 GMT
                Not After : Feb  8 21:41:34 2036 GMT
            Subject: CN = quicp2p.test
            Subject Public Key Info:
                Public Key Algorithm: id-ecPublicKey
                    Public-Key: (256 bit)
                    pub:
                        04:05:b0:54:fa:cb:13:da:ab:49:be:77:c1:97:52:
                        10:1d:f0:2b:ff:04:00:20:d6:53:64:a2:d2:98:40:
                        ed:06:0f:b6:d3:fe:09:3d:08:0b:41:9e:e1:6f:7d:
                        35:d1:7b:45:53:43:b9:d4:54:17:a5:19:e3:0a:5c:
                        3f:c4:c5:65:e4
                    ASN1 OID: prime256v1
                    NIST CURVE: P-256
            X509v3 extensions:
                X509v3 Subject Key Identifier:
                    C6:E1:41:09:BC:81:66:03:8D:ED:1F:DF:AB:DF:29:1A:DA:09:48:F4
                X509v3 Authority Key Identifier:
                    C6:E1:41:09:BC:81:66:03:8D:ED:1F:DF:AB:DF:29:1A:DA:09:48:F4
                X509v3 Basic Constraints: critical
                    CA:TRUE
        Signature Algorithm: ecdsa-with-SHA256
        Signature Value:
            30:46:02:21:00:b9:dd:0a:13:f2:7e:87:1f:08:f9:4d:1b:99:
            3a:90:37:ca:90:4f:73:a2:fe:10:fd:10:dd:6d:b0:8d:f4:9e:
            7d:02:21:00:87:94:0f:a0:4e:5b:2e:0c:9f:a8:a6:01:64:c9:
            f7:98:65:f8:3f:5c:b6:f0:d7:68:1c:59:5f:32:f5:9a:33:4e
        """;

    public const string Base64 = """
                                  MIIBhDCCASmgAwIBAgIUCy2CIbQyFXtgD7FBfoY9uUkUbPEwCgYIKoZIzj0EAwIw
                                  FzEVMBMGA1UEAwwMcXVpY3AycC50ZXN0MB4XDTI2MDIxMDIxNDEzNFoXDTM2MDIw
                                  ODIxNDEzNFowFzEVMBMGA1UEAwwMcXVpY3AycC50ZXN0MFkwEwYHKoZIzj0CAQYI
                                  KoZIzj0DAQcDQgAEBbBU+ssT2qtJvnfBl1IQHfAr/wQAINZTZKLSmEDtBg+20/4J
                                  PQgLQZ7hb3010XtFU0O51FQXpRnjClw/xMVl5KNTMFEwHQYDVR0OBBYEFMbhQQm8
                                  gWYDje0f36vfKRraCUj0MB8GA1UdIwQYMBaAFMbhQQm8gWYDje0f36vfKRraCUj0
                                  MA8GA1UdEwEB/wQFMAMBAf8wCgYIKoZIzj0EAwIDSQAwRgIhALndChPyfocfCPlN
                                  G5k6kDfKkE9zov4Q/RDdbbCN9J59AiEAh5QPoE5bLgyfqKYBZMn3mGX4P1y28Ndo
                                  HFlfMvWaM04=
                                  """;

    private const string Sha256Hash =
        "64:D3:8A:FA:79:3F:49:D8:DA:C4:4D:FC:0C:FF:B2:3F:04:77:DC:F5:D1:BD:45:7A:2C:38:1F:48:8A:68:56:47";

    [Fact]
    public void should_return_fingerprint_as_sha256()
    {
        var cert = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(Base64));
        var wrapper = new QuicPeer.Common.Certificate(cert);

        var fingerprint = wrapper.GetFingerprint();
        var actualHash = string.Join(":", fingerprint.Select(b => $"{b:X2}"));

        Assert.Equal(Sha256Hash, actualHash);
    }

    [Theory]
    [InlineData("02.03.2026", false)]
    [InlineData("02.03.2050", true)]
    public void should_validate_expiration_date(string utcNow, bool isExpired)
    {
        var cert = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(Base64));
        var wrapper = new QuicPeer.Common.Certificate(cert);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow()
            .Returns(DateTime.ParseExact(utcNow, "dd.MM.yyyy", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal));
        
        Assert.Equal(isExpired, wrapper.IsExpired(timeProvider));
    }

    [Fact]
    public void should_export_to_pfx()
    {
        var cert = X509CertificateLoader.LoadCertificate(
            Convert.FromBase64String(Base64));
        var wrapper = new QuicPeer.Common.Certificate(cert);

        var exported = wrapper.GetPfx();
        
        Assert.NotEmpty(exported);
        var exception = Record.Exception(()=>X509CertificateLoader.LoadPkcs12(exported, null));
        Assert.Null(exception);
    }

    [Fact]
    public void should_set_CN_from_options()
    {
        const string CN = "test";

        CertificateOptions options = new()
        {
            CommonName = CN
        };
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_ => new DateTimeOffset(new DateTime(2100, 1,1)));
        var wrapper = new QuicPeer.Common.Certificate(options, timeProvider);
        
        var exported = wrapper.GetPfx();
        var cert = X509CertificateLoader.LoadPkcs12(exported, null);
        
        Assert.Equal($"CN="+CN, cert.Subject);
    }
    
    [Fact]
    public void should_set_expiration_date_from_options()
    {
        var utcNow = new DateTime(2100, 1, 1);
        var lifespan = TimeSpan.FromDays(2);
        var timeProvider = Substitute.For<TimeProvider>();
        timeProvider.GetUtcNow().Returns(_ => new DateTimeOffset(utcNow));
        var options = new CertificateOptions
        {
            CommonName = "test",
            Lifespan = lifespan
        };
        
        var cert = new QuicPeer.Common.Certificate(options, timeProvider);

        var isExpired = cert.IsExpired(timeProvider);
        Assert.False(isExpired);


        utcNow = utcNow.AddDays(1); // Day before expiration
        isExpired = cert.IsExpired(timeProvider);
        Assert.False(isExpired);
        
        utcNow = utcNow.AddDays(2); // Day of expiration
        isExpired = cert.IsExpired(timeProvider);
        Assert.True(isExpired);
        
        utcNow = utcNow.AddDays(3); // Day after expiration
        isExpired = cert.IsExpired(timeProvider);
        Assert.True(isExpired);
    }
}