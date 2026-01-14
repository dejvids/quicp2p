namespace QuicPeer.AppCommands;

public static class AppCommandsExtensions
{
    public static IServiceCollection AddAppCommands(this IServiceCollection services)
    {
        services.AddScoped<ConnectCommand>()
            .AddScoped<ConnectCommand>()
            .AddScoped<SendCommand>()
            .AddScoped<SendFileCommand>()
            .AddScoped<ShowDataCommand>();
        
        return services;
    }
}
