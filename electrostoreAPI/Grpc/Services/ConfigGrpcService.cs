using ElectrostoreAPI.Services.ConfigService;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Grpc.Services;

public class ConfigGrpcService : ConfigGrpc.ConfigGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ConfigGrpcService> _logger;
    private readonly IConfigService _configService;

    public ConfigGrpcService(
        ApplicationDbContext context,
        ILogger<ConfigGrpcService> logger,
        IConfigService configService)
    {
        _context = context;
        _logger = logger;
        _configService = configService;
    }

    public override Task<GetConfigReply> GetConfig(GetConfigRequest request, ServerCallContext context)
    {
        var reply = new GetConfigReply { DemoMode = _configService.GetDemoMode() };
        reply.AllowedImageExtensions.AddRange(_configService.GetAllowedImageExtensions());
        return Task.FromResult(reply);
    }
}
