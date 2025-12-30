using System.Runtime.Versioning;
using Microsoft.Extensions.Options;
using QuicPeer.Client;
using QuicPeer.Options;
using QuicPeer.Server;
using QuicPeer.Logging;
using System.Threading.Channels;
using Serilog;

[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]
[assembly: SupportedOSPlatform("macos")]


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilogLogging();
builder.Services.AddHostedService<PeerServer>();
builder.Services.AddHostedService<PeerConnector>();
builder.Services.AddScoped<IPeerClientFactory, PeerClientFactory>();

builder.Services.AddOptionsWithValidateOnStart<CertificateOptions>()
    .Bind(builder.Configuration.GetSection(CertificateOptions.SectionName));
builder.Services.AddOptionsWithValidateOnStart<ServerOptions>()
.Configure<IOptions<CertificateOptions>>((serverOptions, certificateOptions) =>
{
    serverOptions.ServerCertificate = certificateOptions.Value;
}).Bind(builder.Configuration.GetSection(ServerOptions.SectionName));
builder.Services.AddOptionsWithValidateOnStart<ClientOptions>()
    .Configure<IOptions<CertificateOptions>>((clientOptions, certificateOptions) =>
    {
        clientOptions.ClientCertificate = certificateOptions.Value;
    }).Bind(builder.Configuration.GetSection(ClientOptions.SectionName));

var commandChannel = Channel.CreateBounded<string>(new BoundedChannelOptions(100)
{
    SingleReader = true,
    SingleWriter = true
});

builder.Services.AddSingleton(commandChannel);

var app = builder.Build();
CancellationTokenSource cts = new();

var consoleInputTask = Task.Run(async () =>
{
    Log.Logger.Information("Console input task started.");
    Console.WriteLine("Enter commands (e.g., /connect <IP:Port>, /exit to quit):");
    while (!cts.Token.IsCancellationRequested)
    {
        var input = Console.ReadLine();
        if (input is null)
        {
            continue;
        }
        if (input.Equals("/exit", StringComparison.OrdinalIgnoreCase))
        {
            Log.Logger.Information("Exit command received. Shutting down...");
            Console.WriteLine("Shutting down...");
            cts.Cancel();
            break;
        }
        await commandChannel.Writer.WriteAsync(input, cts.Token);
    }
}, cts.Token);

await app.StartAsync(cts.Token);

await app.WaitForShutdownAsync();
Log.Logger.Information("Application has exited.");

