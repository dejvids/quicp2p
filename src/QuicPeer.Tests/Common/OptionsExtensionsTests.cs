using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using QuicPeer.Options;

namespace QuicPeer.Tests.Common;

public class OptionsExtensionsTests
{
    [Fact]
    public void should_add_server_options()
    {
        var builder = Substitute.For<IHostApplicationBuilder>();
        var services = Substitute.For<IServiceCollection>();
        builder.Services.Returns(services);
        _ = OptionsExtensions.ConfigureOptions(builder);

        services.Received().AddOptionsWithValidateOnStart<ServerOptions>();
    }

    [Fact]
    public void should_add_client_options()
    {
        var builder = Substitute.For<IHostApplicationBuilder>();
        var services = Substitute.For<IServiceCollection>();
        builder.Services.Returns(services);
        _ = OptionsExtensions.ConfigureOptions(builder);

        services.Received().AddOptionsWithValidateOnStart<ClientOptions>();
    }
}