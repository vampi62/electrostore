using ElectrostoreNOTIF.Services.EmailSenderService;
using ElectrostoreNOTIF.Services.WebPushService;
using ElectrostoreNOTIF.Services.ConfigCacheService;
using ElectrostoreNOTIF.Extensions;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using ElectrostoreNOTIF.Grpc;
using ElectrostoreNOTIF.Kafka.Consumers;

namespace ElectrostoreNOTIF;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Configuration.AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true);
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("config/appsettings.Development.json", optional: true, reloadOnChange: true);
        }
        if (builder.Configuration.GetSection("Vault:Enable").Get<bool>())
        {
            var authMethod = new TokenAuthMethodInfo(builder.Configuration.GetSection("Vault:Token").Value);
            var vaultConfig = new VaultClientSettings(builder.Configuration.GetSection("Vault:Addr").Value, authMethod);
            builder.Services.AddSingleton<IVaultClient>(new VaultClient(vaultConfig));
            builder.Configuration.AddVaultConfiguration();
        }

        // gRPC client for the API service
        builder.Services.AddGrpcClient<ConfigGrpc.ConfigGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<UsersGrpc.UsersGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });

        AddScopes(builder);

        var app = builder.Build();

        app.MapGet("/health", (IConfiguration config, ConfigCacheService configCache) =>
            Results.Ok(new
            {
                status = configCache.DemoMode ? "demo" : "healthy",
                smtp = config.GetValue<bool>("Smtp:Enable") ? "configured" : "not configured",
                webPush = config.GetValue<bool>("WebPush:Enable") ? "configured" : "not configured"
            }));

        app.Run();
    }

    private static void AddScopes(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IEmailSenderService, EmailSenderService>();
        builder.Services.AddSingleton<IWebPushService, WebPushService>();
        builder.Services.AddSingleton<ConfigCacheService>();
        builder.Services.AddSingleton<IConfigCacheService>(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService<KafkaNotifConsumer>();
    }
}
