namespace QuicPeer.Options;

public static class OptionsExtensions
{
    public static IHostApplicationBuilder ConfigureOptions(this IHostApplicationBuilder builder)
    {
        var certificateSection = builder.Configuration.GetSection(ClientOptions.SectionName);
        var certificateOptions = certificateSection
            .Get<CertificateOptions>() ??  new CertificateOptions();
        var transferOptions = builder.Configuration.GetSection(TransferOptions.SectionName)?
            .Get<TransferOptions>();

        builder.Services.AddOptionsWithValidateOnStart<CertificateOptions>().Bind(certificateSection);
        builder.Services.AddOptionsWithValidateOnStart<ServerOptions>()
            .Configure(serverOptions =>
            {
                serverOptions.ServerCertificate = certificateOptions;
                var serverTransferSection = builder.Configuration.GetSection(ServerOptions.SectionName)?
                    .GetSection(nameof(ServerOptions.Transfer))?.Get<TransferOptions>();
                if (serverTransferSection is null && transferOptions is not null)
                {
                    serverOptions.Transfer = new ServerTransferOptions { BufferSize = transferOptions.BufferSize };
                }
            }).Bind(builder.Configuration.GetSection(ServerOptions.SectionName));

        builder.Services.AddOptionsWithValidateOnStart<ClientOptions>()
            .Configure(clientOptions =>
            {
                clientOptions.ClientCertificate = certificateOptions;
                var clientTransferSection = builder.Configuration.GetSection(ClientOptions.SectionName)?
                    .GetSection(nameof(ClientOptions.Transfer)).Get<TransferOptions>();
                if (clientTransferSection is null && transferOptions is not null)
                {
                    clientOptions.Transfer = new TransferOptions{ BufferSize = transferOptions.BufferSize };
                }
            })
            .Bind(builder.Configuration.GetSection(ClientOptions.SectionName));

        return builder;
    }
}