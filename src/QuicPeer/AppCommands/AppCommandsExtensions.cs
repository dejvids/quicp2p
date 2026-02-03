namespace QuicPeer.AppCommands;

public static class AppCommandsExtensions
{
    public static IServiceCollection AddAppCommands(this IServiceCollection services)
    {
        services.AddSingleton<ConnectCommand>()
            .AddSingleton<ConnectCommand>()
            .AddSingleton<SendCommand>()
            .AddSingleton<SendFileCommand>()
            .AddSingleton<ShowDataCommand>();
        
        return services;
    }
}
