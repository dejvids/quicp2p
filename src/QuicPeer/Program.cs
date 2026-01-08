using Microsoft.Extensions.Options;
using QuicPeer;
using QuicPeer.Client;
using QuicPeer.Logging;
using QuicPeer.Options;
using QuicPeer.Server;
using QuicPeer.Server.Commands;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]
[assembly: SupportedOSPlatform("macos")]



var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilogLogging();
builder.Services.AddHostedService<PeerServer>();
builder.Services.AddHostedService<ConsoleApp>();
builder.Services.AddSingleton<PeerConnector>();
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

var serverMessageQueue = new ServerMessageQueue();

builder.Services.AddSingleton<IMessageQueue<IServerCommand>, ServerMessageQueue>();

var app = builder.Build();
CancellationTokenSource cts = new();

await app.StartAsync(cts.Token);
