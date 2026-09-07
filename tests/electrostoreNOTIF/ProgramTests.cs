using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using ElectrostoreNOTIF.Grpc;
using ElectrostoreNOTIF.Kafka.Consumers;
using ElectrostoreNOTIF.Services.ConfigCacheService;
using ElectrostoreNOTIF.Services.EmailSenderService;
using ElectrostoreNOTIF.Services.NotificationTemplateService;
using ElectrostoreNOTIF.Services.WebPushService;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace ElectrostoreNOTIF.Tests;

public class ProgramTests
{
    // Program.Main() itself requires a real "config/appsettings.json" file and live infrastructure
    // (Kafka, the API's gRPC endpoints) once the hosted services start, so it isn't exercised
    // directly. AddScopes is the private static method that owns the DI wiring and can be tested
    // in isolation by inspecting the resulting IServiceCollection without building/starting the host.
    private static void InvokePrivateStatic(string methodName, object arg)
    {
        var method = typeof(Program).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException($"{methodName} method not found");
        method.Invoke(null, new[] { arg });
    }

    private static WebApplicationBuilder CreateBuilderWithVapidConfig()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["VAPID:PublicKey"] = "public-key",
            ["VAPID:PrivateKey"] = "private-key"
        });
        return builder;
    }

    // ---------- ConfigureLogging ----------

    [Fact]
    public void ConfigureLogging_ShouldConfigureSimpleConsoleFormatter()
    {
        var builder = WebApplication.CreateBuilder();

        InvokePrivateStatic("ConfigureLogging", builder);

        using var provider = builder.Services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptionsMonitor<SimpleConsoleFormatterOptions>>().CurrentValue;

        Assert.Equal("yyyy-MM-dd HH:mm:ss ", options.TimestampFormat);
        Assert.True(options.SingleLine);
    }

    // ---------- ConfigureConfiguration ----------

    [Fact]
    public void ConfigureConfiguration_ShouldLoadDevelopmentOverride_WhenEnvironmentIsDevelopment()
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var configDir = Path.Combine(tempRoot, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "appsettings.json"), "{\"Foo\":\"Base\"}");
        File.WriteAllText(Path.Combine(configDir, "appsettings.Development.json"), "{\"Foo\":\"Dev\"}");

        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Development
            });

            InvokePrivateStatic("ConfigureConfiguration", builder);

            Assert.Equal("Dev", builder.Configuration["Foo"]);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ConfigureConfiguration_ShouldNotLoadDevelopmentOverride_WhenEnvironmentIsProduction()
    {
        var previousDirectory = Directory.GetCurrentDirectory();
        var tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var configDir = Path.Combine(tempRoot, "config");
        Directory.CreateDirectory(configDir);
        File.WriteAllText(Path.Combine(configDir, "appsettings.json"), "{\"Foo\":\"Base\"}");
        File.WriteAllText(Path.Combine(configDir, "appsettings.Development.json"), "{\"Foo\":\"Dev\"}");

        try
        {
            Directory.SetCurrentDirectory(tempRoot);
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production
            });

            InvokePrivateStatic("ConfigureConfiguration", builder);

            Assert.Equal("Base", builder.Configuration["Foo"]);
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    // ---------- ConfigureVault ----------

    [Fact]
    public void ConfigureVault_ShouldRegisterVaultClient_WhenVaultEnabled()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "true",
            ["Vault:Token"] = "fake-token",
            ["Vault:Addr"] = "http://localhost:8200"
        });

        InvokePrivateStatic("ConfigureVault", builder);

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(VaultSharp.IVaultClient));
    }

    [Fact]
    public void ConfigureVault_ShouldNotRegisterVaultClient_WhenVaultDisabled()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Vault:Enable"] = "false"
        });

        InvokePrivateStatic("ConfigureVault", builder);

        Assert.DoesNotContain(builder.Services, d => d.ServiceType == typeof(VaultSharp.IVaultClient));
    }

    // ---------- ConfigureGrpcClients ----------

    [Fact]
    public void ConfigureGrpcClients_ShouldRegisterBothGrpcClients()
    {
        var builder = WebApplication.CreateBuilder();

        InvokePrivateStatic("ConfigureGrpcClients", builder);

        using var provider = builder.Services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<ConfigGrpc.ConfigGrpcClient>());
        Assert.NotNull(provider.GetService<UsersGrpc.UsersGrpcClient>());
    }

    // ---------- AddScopes (déjà existant) ----------

    [Fact]
    public void AddScopes_ShouldRegisterAllServices_AsSingletons()
    {
        var builder = WebApplication.CreateBuilder();

        InvokePrivateStatic("AddScopes", builder);

        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IEmailSenderService) && d.ImplementationType == typeof(EmailSenderService) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IWebPushService) && d.ImplementationType == typeof(WebPushService) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(INotificationTemplateService) && d.ImplementationType == typeof(NotificationTemplateService) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(ConfigCacheService) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IConfigCacheService) && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddScopes_ShouldRegisterAllExpectedHostedServices()
    {
        var builder = WebApplication.CreateBuilder();

        InvokePrivateStatic("AddScopes", builder);

        var hostedServiceDescriptors = builder.Services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
        Assert.Equal(2, hostedServiceDescriptors.Count);
        Assert.Contains(hostedServiceDescriptors, d => d.ImplementationType == typeof(KafkaNotifConsumer));
    }

    [Fact]
    public void AddScopes_ShouldResolveConfigCacheServiceHostedServiceToSameSingletonInstance()
    {
        var builder = CreateBuilderWithVapidConfig();
        InvokePrivateStatic("AddScopes", builder);
        builder.Services.AddSingleton(new Mock<ConfigGrpc.ConfigGrpcClient>().Object);
        builder.Services.AddSingleton(new Mock<UsersGrpc.UsersGrpcClient>().Object);
        using var provider = builder.Services.BuildServiceProvider();

        var configCacheService = provider.GetRequiredService<ConfigCacheService>();
        var configCacheInterface = provider.GetRequiredService<IConfigCacheService>();
        var hostedConfigCacheService = provider.GetServices<IHostedService>().OfType<ConfigCacheService>().Single();

        Assert.Same(configCacheService, configCacheInterface);
        Assert.Same(configCacheService, hostedConfigCacheService);
    }

    // ---------- MapHealthEndpoint ----------

    private static WebApplicationBuilder CreateTestServerBuilder(bool demoMode, bool smtpEnabled, bool vapidEnabled)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Smtp:Enable"] = smtpEnabled.ToString(),
            ["VAPID:Enable"] = vapidEnabled.ToString()
        });

        var configCacheMock = new Mock<IConfigCacheService>();
        configCacheMock.SetupGet(c => c.DemoMode).Returns(demoMode);
        builder.Services.AddSingleton(configCacheMock.Object);

        return builder;
    }

    [Theory]
    [InlineData(false, true, false, "healthy")]
    [InlineData(true, false, true, "demo")]
    public async Task HealthEndpoint_ShouldReturnExpectedStatusAndFlags(bool demoMode, bool smtpEnabled, bool vapidEnabled, string expectedStatus)
    {
        var builder = CreateTestServerBuilder(demoMode, smtpEnabled, vapidEnabled);
        var app = builder.Build();

        InvokePrivateStatic("MapHealthEndpoint", app);

        await app.StartAsync();
        try
        {
            using var client = app.GetTestClient();
            var response = await client.GetAsync("/health");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal(expectedStatus, json.GetProperty("status").GetString());
            Assert.Equal(smtpEnabled, json.GetProperty("smtp").GetBoolean());
            Assert.Equal(vapidEnabled, json.GetProperty("webPush").GetBoolean());
        }
        finally
        {
            await app.StopAsync();
        }
    }
}
