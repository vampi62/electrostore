using ElectrostoreWORKER.Grpc;
using Grpc.Core;

namespace ElectrostoreWORKER.Services.ConfigCacheService;

public class ConfigCacheService : IHostedService, IConfigCacheService
{
    private readonly ConfigGrpc.ConfigGrpcClient _client;
    private readonly ILogger<ConfigCacheService> _logger;

    public bool DemoMode { get; private set; }

    public ConfigCacheService(ConfigGrpc.ConfigGrpcClient client, ILogger<ConfigCacheService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reply = await _client.GetConfigAsync(new WORKERGetConfigRequest(), cancellationToken: cancellationToken);
            DemoMode = reply.DemoMode;
            _logger.LogInformation("Config loaded from API: DemoMode={DemoMode}", DemoMode);
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Failed to load config from API. Default values applied.");
            DemoMode = false;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
