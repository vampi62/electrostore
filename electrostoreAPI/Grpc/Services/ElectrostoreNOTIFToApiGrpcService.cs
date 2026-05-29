using ElectrostoreAPI.Services.ConfigService;
using ElectrostoreAPI.Services.FileService;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Grpc.Services;

public class ElectrostoreNOTIFToApiGrpcService : NOTIFToAPIGrpc.NOTIFToAPIGrpcBase
{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly ILogger<ElectrostoreNOTIFToApiGrpcService> _logger;
    private readonly IConfigService _configService;

    public ElectrostoreNOTIFToApiGrpcService(
        ApplicationDbContext context,
        IFileService fileService,
        ILogger<ElectrostoreNOTIFToApiGrpcService> logger,
        IConfigService configService)
    {
        _context = context;
        _fileService = fileService;
        _logger = logger;
        _configService = configService;
    }

    public override async Task<GetUserInfoReply> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
    {
        var user = await _context.Users.FindAsync(request.UserId, context.CancellationToken);
        if (user is null)
        {
            return new GetUserInfoReply { Found = false };
        }
        return new GetUserInfoReply { Found = true, Email = user.email_user };
    }

    public override async Task<GetUserPushSubscriptionsReply> GetUserPushSubscriptions(
        GetUserPushSubscriptionsRequest request, ServerCallContext context)
    {
        var rows = await _context.UserPushSubscriptions
            .Where(s => s.id_user == request.UserId)
            .ToListAsync(context.CancellationToken);

        var reply = new GetUserPushSubscriptionsReply();
        reply.Subscriptions.AddRange(rows.Select(s => new PushSubscriptionItem
        {
            Id = s.id_push_subscription,
            Endpoint = s.endpoint,
            P256Dh = s.p256dh,
            Auth = s.auth,
        }));
        return reply;
    }

    public override Task<NOTIFGetConfigReply> GetConfig(NOTIFGetConfigRequest request, ServerCallContext context)
    {
        return Task.FromResult(new NOTIFGetConfigReply { DemoMode = _configService.GetDemoMode() });
    }
}
