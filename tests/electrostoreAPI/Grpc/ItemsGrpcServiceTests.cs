using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Tests.Utils;
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
            .Setup(s => s.GetLowStockItemsAsync(It.IsAny<CancellationToken>()))
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
            .Setup(s => s.GetLowStockItemsAsync(It.IsAny<CancellationToken>()))
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
}
