using System.Net;
using QuicPeer.Client.Exceptions;

namespace QuicPeer.Client;

/// <summary>
/// Provides methods for parsing string representations of network endpoints into <see cref="IPEndPoint"/> instances.
/// </summary>
/// <remarks>This class supports parsing both IP address endpoints and DNS hostnames with optional port numbers.
/// It is intended for scenarios where endpoint information is provided as a string, such as configuration files or user
/// input. The class is static and cannot be instantiated.</remarks>
/// <exception cref="EndpointParsingException">Thrown when the endpoint string cannot be parsed into a valid <see cref="IPEndPoint"/>.</exception>"
public static class EndpointParser
{
    public static IPEndPoint Parse(string endpointString, int defaultPort)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(endpointString))
            {
                throw new EndpointParsingException("Remote endpoint cannot be null or empty");
            }

            if (IPEndPoint.TryParse(endpointString, out var ipEndpoint))
            {
                ipEndpoint.Port = ipEndpoint.Port == 0 ? defaultPort : ipEndpoint.Port;

                return ipEndpoint;
            }

            return TryParseDnsEndpoint(endpointString, defaultPort);
        }
        catch (Exception ex)
        {
            throw new EndpointParsingException("Given endpoint is neither IP or host name", ex);
        }
    }

    private static IPEndPoint TryParseDnsEndpoint(string endpointString, int defaultPort)
    {
        var parts = endpointString.Split(":");
        var host = parts[0];
        var port = parts.Length == 2 && int.TryParse(parts[1], out var portParsed) ? portParsed : defaultPort;

        var iPAddresses = Dns.GetHostAddresses(host);
        if (iPAddresses.Length == 0)
        {
            throw new EndpointParsingException($"Could not resolve DNS for host: {host}");
        }

        return new IPEndPoint(iPAddresses.Last(), port);
    }
}
