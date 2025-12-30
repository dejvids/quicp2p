using QuicPeer.Client;
using QuicPeer.Server;
using Serilog;
using Serilog.Formatting.Json;

namespace QuicPeer.Logging;

internal static class LoggingExtensions
{
    private const long FileSizeLimitBytes = 2_000_000; // 2 MB

    internal static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        var serverNamespace = typeof(PeerServer).Namespace;
        var clientNamespace = typeof(PeerClient).Namespace;
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)

            //General log
            .WriteTo.Logger(cfg => cfg
                .Filter.ByExcluding($"StartsWith(SourceContext, '{serverNamespace}') or StartsWith(SourceContext, '{clientNamespace}')")
                .WriteTo.File("logs\\system.log",  fileSizeLimitBytes: FileSizeLimitBytes))

            //Server log
            .WriteTo.Logger(cfg => cfg
                .Filter.ByIncludingOnly($"StartsWith(SourceContext, '{serverNamespace}')")
                .WriteTo.File(new JsonFormatter(), "logs\\server.log", fileSizeLimitBytes: FileSizeLimitBytes))

            //Client log
            .WriteTo.Logger(cfg => cfg
                .Filter.ByIncludingOnly($"StartsWith(SourceContext, '{clientNamespace}')")
                .WriteTo.File(new JsonFormatter(),"logs\\client.log", fileSizeLimitBytes: FileSizeLimitBytes))

            .CreateLogger();

        services.AddSerilog();


        return services;
    }
}
