using System.IO.Abstractions;
using Microsoft.Extensions.Options;
using QuicPeer;
using QuicPeer.AppCommands;
using QuicPeer.Client;
using QuicPeer.Logging;
using QuicPeer.Options;
using QuicPeer.Server;
using QuicPeer.Server.Commands;
using System.Runtime.Versioning;
using System.Text;
using QuicPeer.Common;
using Spectre.Console;

[assembly: SupportedOSPlatform("windows")]
[assembly: SupportedOSPlatform("linux")]
[assembly: SupportedOSPlatform("macos")]

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilogLogging();
builder.Services.AddHostedService<PeerServer>();
builder.Services.AddHostedService<ConsoleApp>();
builder.Services.AddScoped<PeerConnector>();
builder.Services.AddScoped<IPeerClientFactory, PeerClientFactory>();
builder.Services.AddAppCommands();
builder.Services.AddScoped<IConsoleAccessor, ConsoleAccessor>();
builder.Services.AddScoped<IChecksumProvider, CheckSumProvider>();
builder.Services.AddScoped<IFilesReceiver, FilesReceiver>();
builder.Services.AddScoped<ConnectionManager>();
builder.Services.AddSingleton<IFileSystem>(new FileSystem());

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
builder.Services.AddOptionsWithValidateOnStart<FilesReceiverOptions>()
    .Bind(builder.Configuration.GetSection(ServerOptions.SectionName)
        .GetSection(FilesReceiverOptions.SectionName));

builder.Services.AddSingleton<IMessageQueue<IServerCommand>, ServerMessageQueue>();

var app = builder.Build();
CancellationTokenSource cts = new();

await app.StartAsync(cts.Token);
