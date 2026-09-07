using ElectrostoreCRON.Extensions;
using ElectrostoreCRON.Grpc;
using ElectrostoreCRON.Kafka.Consumers;
using ElectrostoreCRON.Kafka.Producer;
using ElectrostoreCRON.Services.ConfigCacheService;
using ElectrostoreCRON.Services.CronJobExecutionRegistry;
using ElectrostoreCRON.Services.CronSchedulerService;

using ElectrostoreCRON.Services.ItemMovementReportService;
using ElectrostoreCRON.Services.StockLowAlertService;
using Quartz;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace ElectrostoreCRON;

public static partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        ConfigureConfiguration(builder);
        ConfigureVault(builder);
        ConfigureLogging(builder);
        ConfigureGrpcClients(builder);

        // Quartz.NET scheduler
        builder.Services.AddQuartz();
        builder.Services.AddQuartzHostedService(opt => opt.WaitForJobsToComplete = true);

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
        builder.Services.AddGrpcClient<ConfigGrpc.ConfigGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<CronJobsGrpc.CronJobsGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<ItemsHistoryGrpc.ItemsHistoryGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
        builder.Services.AddGrpcClient<ItemsGrpc.ItemsGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration["ApiServiceGrpcUrl"] ?? "http://electrostoreAPI:5001");
        });
    }

    private static void AddScopes(WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient();
        builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
        builder.Services.AddSingleton<IItemMovementReportService, ItemMovementReportService>();
        builder.Services.AddSingleton<IStockLowAlertService, StockLowAlertService>();
        builder.Services.AddSingleton<ICronJobExecutionRegistry, CronJobExecutionRegistry>();
        builder.Services.AddSingleton<ConfigCacheService>();
        builder.Services.AddSingleton<IConfigCacheService>(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService<CronSchedulerService>();
        builder.Services.AddHostedService<KafkaCronJobEventsConsumer>();
        builder.Services.AddTransient<ElectrostoreCronJob>();
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
