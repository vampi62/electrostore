using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Services.UserPushSubscriptionService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class UsersGrpcService : UsersGrpc.UsersGrpcBase
{
    private readonly IUserService _userService;
    private readonly IUserPushSubscriptionService _userPushSubscriptionService;
    private readonly ILogger<UsersGrpcService> _logger;

    public UsersGrpcService(
        IUserService userService,
        IUserPushSubscriptionService userPushSubscriptionService,
        ILogger<UsersGrpcService> logger)
    {
        _logger = logger;
        _userService = userService;
        _userPushSubscriptionService = userPushSubscriptionService;
    }

    public override async Task<GetUserInfoReply> GetUserInfo(GetUserInfoRequest request, ServerCallContext context)
    {
        var user = await _userService.GetUserByIdAsync(request.UserId, context.CancellationToken);
        if (user is null)
        {
            return new GetUserInfoReply { Found = false };
        }
        return new GetUserInfoReply { Found = true, Email = user.email_user };
    }

    public override async Task<GetUserPushSubscriptionsReply> GetUserPushSubscriptions(
        GetUserPushSubscriptionsRequest request, ServerCallContext context)
    {
        var rows = await _userPushSubscriptionService.GetPushSubscriptionsByUserIdAsync(request.UserId, context.CancellationToken);
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
}
