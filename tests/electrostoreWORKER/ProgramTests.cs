using System.Reflection;
using ElectrostoreWORKER.Grpc;
using ElectrostoreWORKER.Kafka.Consumers;
using ElectrostoreWORKER.Mqtt;
using ElectrostoreWORKER.Services.ConfigCacheService;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace ElectrostoreWORKER.Tests;

public class ProgramTests
{
    // Program.Main() itself requires a real "config/appsettings.json" file and live infrastructure
    // (Kafka, Docker, MQTT broker) once the hosted services start, so it isn't exercised directly.
    // AddScopes is the private static method that owns the DI wiring and can be tested in isolation
    // by inspecting the resulting IServiceCollection without building/starting the host.
    private static void InvokeAddScopes(WebApplicationBuilder builder)
    {
        var method = typeof(Program).GetMethod("AddScopes", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("AddScopes method not found");
        method.Invoke(null, new object[] { builder });
    }

    [Fact]
    public void AddScopes_ShouldRegisterConfigCacheService_AsSingletonAndAsItsInterface()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        InvokeAddScopes(builder);

        // Assert
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(ConfigCacheService) && d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(builder.Services, d => d.ServiceType == typeof(IConfigCacheService) && d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddScopes_ShouldRegisterAllExpectedHostedServices()
    {
        // Arrange
        var builder = WebApplication.CreateBuilder();

        // Act
        InvokeAddScopes(builder);

        // Assert - ConfigCacheService (via factory) + the 2 Kafka consumers + the MQTT client
        var hostedServiceDescriptors = builder.Services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
        Assert.Equal(4, hostedServiceDescriptors.Count);
        Assert.Contains(hostedServiceDescriptors, d => d.ImplementationType == typeof(KafkaMqttUserConsumer));
        Assert.Contains(hostedServiceDescriptors, d => d.ImplementationType == typeof(KafkaTrackingResultConsumer));
        Assert.Contains(hostedServiceDescriptors, d => d.ImplementationType == typeof(MqttClientService));
    }

    [Fact]
    public void AddScopes_ShouldResolveConfigCacheServiceHostedServiceToSameSingletonInstance()
    {
        // The ConfigCacheService hosted-service registration uses a factory that must resolve to
        // the very same singleton instance also exposed as IConfigCacheService, so the cache state
        // observed by consumers matches the instance the host actually starts/stops.
        // Arrange
        var builder = WebApplication.CreateBuilder();
        InvokeAddScopes(builder);
        // The hosted services depend on generated gRPC clients, which AddScopes doesn't register
        // (Program.Main wires them separately via AddGrpcClient) - supply mocks so that resolving
        // IHostedService (which activates every registered hosted service) succeeds.
        builder.Services.AddSingleton(new Mock<ConfigGrpc.ConfigGrpcClient>().Object);
        builder.Services.AddSingleton(new Mock<CommandsGrpc.CommandsGrpcClient>().Object);
        builder.Services.AddSingleton(new Mock<StoresMqttGrpc.StoresMqttGrpcClient>().Object);
        using var provider = builder.Services.BuildServiceProvider();

        // Act
        var configCacheService = provider.GetRequiredService<ConfigCacheService>();
        var configCacheInterface = provider.GetRequiredService<IConfigCacheService>();
        var hostedConfigCacheService = provider.GetServices<IHostedService>().OfType<ConfigCacheService>().Single();

        // Assert
        Assert.Same(configCacheService, configCacheInterface);
        Assert.Same(configCacheService, hostedConfigCacheService);
    }
}
