using ElectrostoreWORKER.Extensions;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Mqtt;
using ElectrostoreWORKER.Services.ConfigCacheService;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace ElectrostoreWORKER;

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

        // gRPC client to the API
        /* builder.Services.AddGrpcClient<CommandsGrpc.CommandsGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        }); */
        builder.Services.AddGrpcClient<ConfigGrpc.ConfigGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<IaTrainingGrpc.IaTrainingGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<StoresMqttGrpc.StoresMqttGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });

        AddScopes(builder);

        var app = builder.Build();

        app.MapGet("/health", (ConfigCacheService configCache) =>
            Results.Ok(new
            {
                status = configCache.DemoMode ? "demo" : "healthy"
            }));

        app.Run();
    }

    private static void AddScopes(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<ConfigCacheService>();
        builder.Services.AddSingleton<IConfigCacheService>(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCacheService>());
        //builder.Services.AddHostedService<KafkaCronCommandConsumer>();
        builder.Services.AddHostedService<KafkaIaStatusConsumer>();
        builder.Services.AddHostedService<MqttClientService>();
    }
}
