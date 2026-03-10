namespace QuicPeer.AppCommands;

public static class AppCommandsExtensions
{
    public static IServiceCollection AddAppCommands(this IServiceCollection services)
    {
        services.AddKeyedSingleton<AppCommand, ConnectCommand>(ConsoleApp.MainMenu)
            .AddKeyedSingleton<AppCommand,SendCommand>(ConnectCommand.ConnectMenu)
            .AddKeyedSingleton<AppCommand, SendFileCommand>(ConnectCommand.ConnectMenu)
            .AddKeyedSingleton<AppCommand, ShowDataCommand>(ConsoleApp.MainMenu)
            .AddSingleton<UnlockCommand>();
        
        return services;
    }
}
