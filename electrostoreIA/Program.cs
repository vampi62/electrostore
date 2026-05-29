using ElectrostoreIA.Services.ConfigCacheService;
using ElectrostoreIA.Services.FileService;
using ElectrostoreIA.Services.ModelTrainerService;
using ElectrostoreIA.Services.ImageDetectorService;
using ElectrostoreIA.Extensions;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using ElectrostoreIA.Grpc;
using ElectrostoreIA.Grpc.Services;
using ElectrostoreIA.Kafka.Consumers;
using ElectrostoreIA.Kafka.Producer;
using Minio;

namespace ElectrostoreIA;

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

        // gRPC server
        builder.Services.AddGrpc(options =>
        {
            //options.Interceptors.Add<AuthInterceptor>();
            options.MaxReceiveMessageSize = 100 * 1024 * 1024; // 50 MB for image data
        });

        // gRPC client for the API service
        builder.Services.AddGrpcClient<IAToAPIGrpc.IAToAPIGrpcClient>(options =>
        {
            options.Address = new Uri(
                builder.Configuration.GetValue<string>("ApiServiceGrpcUrl") ?? "http://electrostoreAPI:5001");
        });

        AddScopes(builder);

        var app = builder.Build();

        app.UseStaticFiles();
        CreateRequiredDirectories();

        app.MapGet("/health", (ModelTrainerService trainerService, ConfigCacheService configCache) =>
            Results.Ok(new
            {
                status = configCache.DemoMode ? "demo" : "healthy",
                training_in_progress = trainerService.IsTrainingInProgress()
            }));

        app.MapGrpcService<IAGrpcService>();

        app.Run();
    }

    private static void AddScopes(WebApplicationBuilder builder)
    {
        if (builder.Configuration.GetSection("S3:Enable").Get<bool>())
        {
            builder.Services.AddSingleton<IMinioClient>(sp =>
            {
                var minioClient = new MinioClient()
                    .WithEndpoint(builder.Configuration.GetSection("S3:Endpoint").Value ?? "localhost:9000")
                    .WithCredentials(
                        builder.Configuration.GetSection("S3:AccessKey").Value ?? "minioadmin",
                        builder.Configuration.GetSection("S3:SecretKey").Value ?? "minioadmin")
                    .WithRegion(builder.Configuration.GetSection("S3:Region").Value ?? "us-east-1")
                    .WithSSL(builder.Configuration.GetSection("S3:Secure").Get<bool>())
                    .Build();
                return minioClient;
            });
        }
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton<IModelTrainerService, ModelTrainerService>();
        builder.Services.AddSingleton<IImageDetectorService, ImageDetectorService>();
        builder.Services.AddSingleton<ConfigCacheService>();
        builder.Services.AddSingleton<IConfigCacheService>(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddHostedService(sp => sp.GetRequiredService<ConfigCacheService>());
        builder.Services.AddSingleton<IKafkaProducerService, KafkaProducerService>();
        builder.Services.AddHostedService<KafkaIaTrainConsumer>();
    }

    private static void CreateRequiredDirectories()
    {
        if (!Directory.Exists("wwwroot/images"))
        {
            Directory.CreateDirectory("wwwroot/images");
        }
        if (!Directory.Exists("wwwroot/models"))
        {
            Directory.CreateDirectory("wwwroot/models");
        }
    }
}
