using ElectrostoreCRON.Extensions;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Kafka.Consumers;
using ElectrostoreCRON.Kafka.Producer;
using ElectrostoreCRON.Services.ConfigCacheService;
using ElectrostoreCRON.Services.CronSchedulerService;
using ElectrostoreCRON.Services.ParcelTrackerService;
using Quartz;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace ElectrostoreCRON;

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
        builder.Services.AddGrpcClient<ConfigGrpc.ConfigGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<CronJobGrpc.CronJobGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });

        // Quartz.NET scheduler
        builder.Services.AddQuartz();
        builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

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
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
        builder.Services.AddSingleton<IParcelTrackerService, ParcelTrackerService>();
        builder.Services.AddSingleton<ConfigCacheService>();
        builder.Services.AddSingleton<IConfigCacheService>(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService<CronSchedulerService>();
        builder.Services.AddHostedService<KafkaCronJobEventsConsumer>();
        builder.Services.AddTransient<ElectrostoreCronJob>();
    }
}
