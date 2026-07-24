using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Services.UserPushSubscriptionService;
using ElectrostoreAPI.Tests.Utils;

namespace ElectrostoreAPI.Tests.Grpc.Services;

public class UsersGrpcServiceTests
{
    private readonly Mock<IUserService> _userService;
    private readonly Mock<IUserPushSubscriptionService> _userPushSubscriptionService;
    private readonly UsersGrpcService _service;

    public UsersGrpcServiceTests()
    {
        _userService = new Mock<IUserService>();
        _userPushSubscriptionService = new Mock<IUserPushSubscriptionService>();
        _service = new UsersGrpcService(_userService.Object, _userPushSubscriptionService.Object, new LoggerFactory().CreateLogger<UsersGrpcService>());
    }

    private static ReadUserDto BuildUser(int id, string email)
    {
        return new ReadUserDto
        {
            id_user = id,
            nom_user = "Doe",
            prenom_user = "John",
            email_user = email,
            role_user = UserRole.User,
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task GetUserInfo_ShouldReturnFound_WhenUserExists()
    {
        // Arrange
        _userService.Setup(s => s.GetUserByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildUser(1, "john@example.com"));

        // Act
        var reply = await _service.GetUserInfo(new GetUserInfoRequest { UserId = 1 }, TestServerCallContext.Create());

        // Assert
        Assert.True(reply.Found);
        Assert.Equal("john@example.com", reply.Email);
    }

    [Fact]
    public async Task GetUserInfo_ShouldReturnNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _userService.Setup(s => s.GetUserByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ReadUserDto?)null);

        // Act
        var reply = await _service.GetUserInfo(new GetUserInfoRequest { UserId = 999 }, TestServerCallContext.Create());

        // Assert
        Assert.False(reply.Found);
        Assert.Equal(string.Empty, reply.Email);
    }

    [Fact]
    public async Task GetUserPushSubscriptions_ShouldReturnEmpty_WhenUserHasNoSubscriptions()
    {
        // Arrange
        _userPushSubscriptionService.Setup(s => s.GetPushSubscriptionsByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadUserPushSubscriptionDto>());

        // Act
        var reply = await _service.GetUserPushSubscriptions(new GetUserPushSubscriptionsRequest { UserId = 1 }, TestServerCallContext.Create());

        // Assert
        Assert.Empty(reply.Subscriptions);
    }

    [Fact]
    public async Task GetUserPushSubscriptions_ShouldMapAllFields()
    {
        // Arrange
        _userPushSubscriptionService.Setup(s => s.GetPushSubscriptionsByUserIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReadUserPushSubscriptionDto>
            {
                new ReadUserPushSubscriptionDto
                {
                    id_push_subscription = 5,
                    id_user = 1,
                    endpoint = "https://push.example.com/1",
                    p256dh = "key",
                    auth = "auth",
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                }
            });

        // Act
        var reply = await _service.GetUserPushSubscriptions(new GetUserPushSubscriptionsRequest { UserId = 1 }, TestServerCallContext.Create());

        // Assert
        var subscription = Assert.Single(reply.Subscriptions);
        Assert.Equal(5, subscription.Id);
        Assert.Equal("https://push.example.com/1", subscription.Endpoint);
        Assert.Equal("key", subscription.P256Dh);
        Assert.Equal("auth", subscription.Auth);
    }
}
