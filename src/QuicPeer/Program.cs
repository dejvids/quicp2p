using System.IO.Abstractions;
using QuicPeer;
using QuicPeer.AppCommands;
using QuicPeer.Client;
using QuicPeer.Logging;
using QuicPeer.Options;
using QuicPeer.Server;
using QuicPeer.Server.Commands;
using QuicPeer.Common;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilogLogging();
builder.Services.AddHostedService<PeerServer>();
builder.Services.AddHostedService<ConsoleApp>();
builder.Services.AddScoped<PeerConnector>();
builder.Services.AddSingleton<IPeerClientFactory, PeerClientFactory>();
builder.Services.AddAppCommands();
builder.Services.AddSingleton<IConsoleAccessor, ConsoleAccessor>();
builder.Services.AddSingleton<IChecksumProvider, CheckSumProvider>();
builder.Services.AddScoped<IFilesReceiver, FilesReceiver>();
builder.Services.AddScoped<ConnectionManager>();
builder.Services.AddSingleton<IFileSystem>(new FileSystem());
builder.Services.AddSingleton<IMessageQueue<IServerCommand>, ServerMessageQueue>();
builder.Services.AddSingleton<IPeersStore, PeersStore>();
builder.ConfigureOptions();

var app = builder.Build();
await app.StartAsync();
await app.WaitForShutdownAsync();