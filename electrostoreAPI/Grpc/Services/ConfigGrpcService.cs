using ElectrostoreAPI.Services.ConfigService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class ConfigGrpcService : ConfigGrpc.ConfigGrpcBase
{
    private readonly IConfigService _configService;
    private readonly ILogger<ConfigGrpcService> _logger;

    public ConfigGrpcService(
        IConfigService configService,
        ILogger<ConfigGrpcService> logger)
    {
        _configService = configService;
        _logger = logger;
    }

    public override Task<GetConfigReply> GetConfig(GetConfigRequest request, ServerCallContext context)
    {
        _logger.LogDebug("GetConfig requested by {Peer}", context.Peer);
        var reply = new GetConfigReply { DemoMode = _configService.GetDemoMode(), AllowedImageExtensions = { _configService.GetAllowedImageExtensions() } };
        return Task.FromResult(reply);
    }
}
