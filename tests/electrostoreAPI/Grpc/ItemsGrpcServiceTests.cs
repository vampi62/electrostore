using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Tests.Utils;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Grpc;

public class ItemsGrpcServiceTests
{
    private readonly Mock<IItemService> _itemService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ILogger<ItemsGrpcService>> _logger = new();

    private ItemsGrpcService CreateService() =>
        new(_itemService.Object, _userService.Object, _logger.Object);

    private static ReadItemDto BuildLowStockItem() => new()
    {
        id_item = 42,
        reference_name_item = "R10K",
        friendly_name_item = "Resistor 10k",
        threshold_min_item = 10,
        quantity_item = 2
    };

    private static ReadUserDto BuildAdmin(int id = 3) => new()
    {
        id_user = id,
        name_user = "Lovelace",
        firstname_user = "Ada",
        email_user = $"admin{id}@example.com",
        role_user = UserRole.Admin
    };

    [Fact]
    public async Task GetLowStockItems_ShouldReturnItemsAndAdministrators()
    {
        // Arrange
        var service = CreateService();
        _itemService
            .Setup(s => s.GetLowStockItemsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildLowStockItem()]);
        _userService
            .Setup(s => s.GetUsersByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildAdmin(), BuildAdmin(4)]);

        // Act
        var reply = await service.GetLowStockItems(new GetLowStockItemsRequest(), TestServerCallContext.Create());

        // Assert
        var item = Assert.Single(reply.Items);
        Assert.Equal(42, item.IdItem);
        Assert.Equal("R10K", item.ReferenceNameItem);
        Assert.Equal("Resistor 10k", item.FriendlyNameItem);
        Assert.Equal(2, item.QuantityItem);
        Assert.Equal(10, item.ThresholdMinItem);
        Assert.Equal(2, reply.Recipients.Count);
        Assert.Equal("admin3@example.com", reply.Recipients[0].Email);
        Assert.Equal("Ada", reply.Recipients[0].Firstname);
        Assert.Equal("Lovelace", reply.Recipients[0].Name);
    }

    [Fact]
    public async Task GetLowStockItems_ShouldReturnEmptyLists_WhenNothingIsLowOrNoAdministrator()
    {
        // Arrange
        var service = CreateService();
        _itemService
            .Setup(s => s.GetLowStockItemsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _userService
            .Setup(s => s.GetUsersByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var reply = await service.GetLowStockItems(new GetLowStockItemsRequest(), TestServerCallContext.Create());

        // Assert
        Assert.Empty(reply.Items);
        Assert.Empty(reply.Recipients);
    }

    [Fact]
    public async Task GetLowStockItems_ShouldForwardParsedSinceDate_WhenProvided()
    {
        // Arrange
        var service = CreateService();
        var sinceDate = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc);
        DateTime? forwarded = null;
        _itemService
            .Setup(s => s.GetLowStockItemsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime?, CancellationToken>((d, _) => forwarded = d)
            .ReturnsAsync([]);
        _userService
            .Setup(s => s.GetUsersByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await service.GetLowStockItems(
            new GetLowStockItemsRequest { SinceDate = sinceDate.ToString("o") },
            TestServerCallContext.Create());

        // Assert
        Assert.Equal(sinceDate, forwarded);
    }

    [Fact]
    public async Task GetLowStockItems_ShouldForwardNullSinceDate_WhenNotProvided()
    {
        // Arrange
        var service = CreateService();
        var forwarded = (DateTime?)DateTime.UtcNow;
        _itemService
            .Setup(s => s.GetLowStockItemsAsync(It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime?, CancellationToken>((d, _) => forwarded = d)
            .ReturnsAsync([]);
        _userService
            .Setup(s => s.GetUsersByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await service.GetLowStockItems(new GetLowStockItemsRequest(), TestServerCallContext.Create());

        // Assert
        Assert.Null(forwarded);
    }

    [Fact]
    public async Task GetLowStockItems_ShouldThrowInvalidArgument_WhenSinceDateIsNotIso8601()
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<RpcException>(() => service.GetLowStockItems(
            new GetLowStockItemsRequest { SinceDate = "last-monday" }, TestServerCallContext.Create()));

        // Assert
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }
}
