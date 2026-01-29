using System.IO.Abstractions;
using QuicPeer;
using QuicPeer.AppCommands;
using QuicPeer.Client;
using QuicPeer.Logging;
using QuicPeer.Options;
using QuicPeer.Server;
using QuicPeer.Server.Commands;
using System.Runtime.Versioning;
using QuicPeer.Common;

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
builder.Services.AddSingleton<IMessageQueue<IServerCommand>, ServerMessageQueue>();
builder.ConfigureOptions();
var app = builder.Build();
CancellationTokenSource cts = new();

await app.StartAsync(cts.Token);
