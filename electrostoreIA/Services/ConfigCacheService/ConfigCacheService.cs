using ElectrostoreIA.Grpc;
using Grpc.Core;

namespace ElectrostoreIA.Services.ConfigCacheService;

public class ConfigCacheService : IHostedService, IConfigCacheService
{
    private readonly IAToAPIGrpc.IAToAPIGrpcClient _client;
    private readonly ILogger<ConfigCacheService> _logger;

    public bool DemoMode { get; private set; }
    public IEnumerable<string> AllowedImageExtensions { get; private set; } = [];

    public ConfigCacheService(IAToAPIGrpc.IAToAPIGrpcClient client, ILogger<ConfigCacheService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            var reply = await _client.GetConfigAsync(new IAGetConfigRequest(), cancellationToken: cancellationToken);
            DemoMode = reply.DemoMode;
            AllowedImageExtensions = reply.AllowedImageExtensions;
            _logger.LogInformation(
                "Config loaded from API at startup: DemoMode={DemoMode}, AllowedImageExtensions=[{AllowedImageExtensions}]",
                DemoMode, string.Join(", ", AllowedImageExtensions));
        }
        catch (RpcException ex)
        {
            _logger.LogError(ex, "Failed to load config from API at startup. Using default values.");
            DemoMode = true; // Default to true if API call fails, as a safe fallback for IA
            AllowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" }; // Default allowed extensions if API call fails
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
