using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.UserPushSubscriptionService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Tests.Utils;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Grpc;

public class UsersGrpcServiceTests
{
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<IUserPushSubscriptionService> _pushService = new();
    private readonly Mock<ILogger<UsersGrpcService>> _logger = new();

    private UsersGrpcService CreateService() => new(_userService.Object, _pushService.Object, _logger.Object);

    [Fact]
    public async Task GetUserInfo_ShouldReturnFoundAndEmail_WhenUserExists()
    {
        _userService
            .Setup(s => s.GetUserByIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReadUserDto
            {
                id_user = 4,
                name_user = "Lovelace",
                firstname_user = "Ada",
                email_user = "ada@example.com",
                role_user = UserRole.Admin
            });
        var service = CreateService();

        var reply = await service.GetUserInfo(new GetUserInfoRequest { UserId = 4 }, TestServerCallContext.Create());

        Assert.True(reply.Found);
        Assert.Equal("ada@example.com", reply.Email);
    }

    [Fact]
    public async Task GetUserInfo_ShouldReturnNotFound_WhenUserIsMissing()
    {
        _userService
            .Setup(s => s.GetUserByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadUserDto?)null);
        var service = CreateService();

        var reply = await service.GetUserInfo(new GetUserInfoRequest { UserId = 404 }, TestServerCallContext.Create());

        Assert.False(reply.Found);
        Assert.Equal(string.Empty, reply.Email);
    }

    [Fact]
    public async Task GetUserPushSubscriptions_ShouldMapSubscriptions()
    {
        _pushService
            .Setup(s => s.GetPushSubscriptionsByUserIdAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadUserPushSubscriptionDto>
            {
                new() { id_user_push_subscription = 1, id_user = 4, endpoint = "https://push/1", p256dh = "key1", auth = "auth1" },
                new() { id_user_push_subscription = 2, id_user = 4, endpoint = "https://push/2", p256dh = "key2", auth = "auth2" }
            });
        var service = CreateService();

        var reply = await service.GetUserPushSubscriptions(new GetUserPushSubscriptionsRequest { UserId = 4 }, TestServerCallContext.Create());

        Assert.Equal(2, reply.Subscriptions.Count);
        Assert.Equal(1, reply.Subscriptions[0].Id);
        Assert.Equal("https://push/1", reply.Subscriptions[0].Endpoint);
        Assert.Equal("key1", reply.Subscriptions[0].P256Dh);
        Assert.Equal("auth1", reply.Subscriptions[0].Auth);
        Assert.Equal(2, reply.Subscriptions[1].Id);
    }

    [Fact]
    public async Task GetUserPushSubscriptions_ShouldReturnEmpty_WhenUserHasNone()
    {
        _pushService
            .Setup(s => s.GetPushSubscriptionsByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadUserPushSubscriptionDto>());
        var service = CreateService();

        var reply = await service.GetUserPushSubscriptions(new GetUserPushSubscriptionsRequest { UserId = 1 }, TestServerCallContext.Create());

        Assert.Empty(reply.Subscriptions);
    }
}
