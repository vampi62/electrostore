using ElectrostoreNOTIF.Services.EmailSenderService;
using ElectrostoreNOTIF.Services.NotificationTemplateService;
using ElectrostoreNOTIF.Services.WebPushService;
using ElectrostoreNOTIF.Services.ConfigCacheService;
using ElectrostoreNOTIF.Extensions;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using ElectrostoreNOTIF.Grpc;
using ElectrostoreNOTIF.Kafka.Consumers;

namespace ElectrostoreNOTIF;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureConfiguration(builder);
        ConfigureVault(builder);
        ConfigureLogging(builder);
        ConfigureGrpcClients(builder);
        AddScopes(builder);

        var app = builder.Build();

        MapHealthEndpoint(app);

        app.Run();
    }

    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Logging.AddSimpleConsole(options =>
        {
            options.TimestampFormat = "yyyy-MM-dd HH:mm:ss ";
            options.SingleLine = true;
        });
    }

    private static void ConfigureConfiguration(WebApplicationBuilder builder)
    {
        builder.Configuration.AddJsonFile("config/appsettings.json", optional: false, reloadOnChange: true);
        if (builder.Environment.IsDevelopment())
        {
            builder.Configuration.AddJsonFile("config/appsettings.Development.json", optional: true, reloadOnChange: true);
        }
    }

    private static void ConfigureVault(WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetSection("Vault:Enable").Get<bool>())
        {
            return;
        }

        var authMethod = new TokenAuthMethodInfo(builder.Configuration.GetSection("Vault:Token").Value);
        var vaultConfig = new VaultClientSettings(builder.Configuration.GetSection("Vault:Addr").Value, authMethod);
        builder.Services.AddSingleton<IVaultClient>(new VaultClient(vaultConfig));
        builder.Configuration.AddVaultConfiguration();
    }

    private static void ConfigureGrpcClients(WebApplicationBuilder builder)
    {
        var apiServiceGrpcUrl = builder.Configuration["ApiServiceGrpcUrl"]
            ?? throw new InvalidOperationException("ApiServiceGrpcUrl configuration is missing.");

        builder.Services.AddGrpcClient<ConfigGrpc.ConfigGrpcClient>(options =>
        {
            options.Address = new Uri(apiServiceGrpcUrl);
        });
        builder.Services.AddGrpcClient<UsersGrpc.UsersGrpcClient>(options =>
        {
            options.Address = new Uri(apiServiceGrpcUrl);
        });
    }

    private static void AddScopes(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IEmailSenderService, EmailSenderService>();
        builder.Services.AddSingleton<IWebPushService, WebPushService>();
        builder.Services.AddSingleton<INotificationTemplateService, NotificationTemplateService>();
        builder.Services.AddSingleton<ConfigCacheService>();
        builder.Services.AddSingleton<IConfigCacheService>(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService<KafkaNotifConsumer>();
    }

    private static void MapHealthEndpoint(WebApplication app)
    {
        app.MapGet("/health", (IConfiguration config, IConfigCacheService configCache) =>
            Results.Ok(new
            {
                status = configCache.DemoMode ? "demo" : "healthy",
                smtp = config.GetValue<bool>("Smtp:Enable"),
                webPush = config.GetValue<bool>("VAPID:Enable")
            }));
    }
}
