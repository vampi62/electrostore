using ElectrostoreWORKER.Extensions;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Mqtt;
using ElectrostoreWORKER.Services.ConfigCacheService;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace ElectrostoreWORKER;

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
        builder.Services.AddGrpcClient<CommandsGrpc.CommandsGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<ConfigGrpc.ConfigGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<StoresMqttGrpc.StoresMqttGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
    }

    private static void AddScopes(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ConfigCacheService>();
        builder.Services.AddSingleton<IConfigCacheService>(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService<KafkaMqttUserConsumer>();
        builder.Services.AddHostedService<MqttClientService>();
    }

    private static void MapHealthEndpoint(WebApplication app)
    {
        app.MapGet("/health", (IConfigCacheService configCache) =>
            Results.Ok(new
            {
                status = configCache.DemoMode ? "demo" : "healthy"
            }));
    }
}
