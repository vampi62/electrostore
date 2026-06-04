using ElectrostoreAPI.Services.ConfigService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class ConfigGrpcService : ConfigGrpc.ConfigGrpcBase
{
    private readonly ILogger<ConfigGrpcService> _logger;
    private readonly IConfigService _configService;

    public ConfigGrpcService(
        ILogger<ConfigGrpcService> logger,
        IConfigService configService)
    {
        _logger = logger;
        _configService = configService;
    }

    public override Task<GetConfigReply> GetConfig(GetConfigRequest request, ServerCallContext context)
    {
        var reply = new GetConfigReply { DemoMode = _configService.GetDemoMode(), AllowedImageExtensions = { _configService.GetAllowedImageExtensions() } };
        return Task.FromResult(reply);
    }
}
