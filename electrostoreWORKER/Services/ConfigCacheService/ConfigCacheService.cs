using ElectrostoreWORKER.Grpc;
using Grpc.Core;

namespace ElectrostoreWORKER.Services.ConfigCacheService;

public class ConfigCacheService : IHostedService, IConfigCacheService
{
    private readonly ConfigGrpc.ConfigGrpcClient _client;
    private readonly ILogger<ConfigCacheService> _logger;
    private readonly int _maxAttempts;
    private readonly TimeSpan _retryDelay;

    public bool DemoMode { get; private set; }

    public ConfigCacheService(ConfigGrpc.ConfigGrpcClient client, ILogger<ConfigCacheService> logger, IConfiguration? configuration = null)
    {
        _client = client;
        _logger = logger;
        _maxAttempts = configuration?.GetValue<int?>("Startup:MaxRetryAttempts") ?? 10;
        _retryDelay = TimeSpan.FromSeconds(configuration?.GetValue<int?>("Startup:RetryDelaySeconds") ?? 5);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                var reply = await _client.GetConfigAsync(new GetConfigRequest(), cancellationToken: cancellationToken);
                DemoMode = reply.DemoMode;
                _logger.LogInformation("Config loaded from API at startup: DemoMode={DemoMode}", DemoMode);
                return;
            }
            catch (RpcException ex)
            {
                if (attempt == _maxAttempts)
                {
                    _logger.LogError(ex, "Failed to load config from API at startup after {Attempts} attempt(s). Using default values.", attempt);
                    DemoMode = true; // Default to true if API call fails
                    return;
                }
                _logger.LogWarning(ex, "Failed to load config from API at startup (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}s...", attempt, _maxAttempts, _retryDelay.TotalSeconds);
                await Task.Delay(_retryDelay, cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
