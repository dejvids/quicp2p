using System.Net;
using System.Net.Sockets;

namespace QuicPeer;

public static class EndpointParser
{
    public static bool TryParse(string endpointString, int defaultPort, out IPEndPoint? endpoint)
    {
        endpoint = null;
        if (string.IsNullOrWhiteSpace(endpointString))
        {
            return false;
        }

        if (IPEndPoint.TryParse(endpointString, out var ipEndpoint))
        {
            ipEndpoint.Port = ipEndpoint.Port == 0 ? defaultPort : ipEndpoint.Port;
            
            endpoint = ipEndpoint;
            return true;
        }

        return TryParseDnsEndpoint(endpointString, defaultPort, out endpoint);

       
    }

    private static bool TryParseDnsEndpoint(string endpointString, int defaultPort, out IPEndPoint? endpoint)
    {
        var parts = endpointString.Split(":");
        var host = parts[0];
        var port = parts.Length == 2 && int.TryParse(parts[1], out var portParsed) ? portParsed : defaultPort;

        try
        {
            var iPAddresses = Dns.GetHostAddresses(host);
            if (iPAddresses.Length == 0)
            {
                endpoint = null;
                return false;
            }

            endpoint = new IPEndPoint(iPAddresses.Last(), port);
            return true;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"DNS resolution failed for host '{host}': {ex.SocketErrorCode}");
            endpoint = null;
            return false;
        }
    }
}
