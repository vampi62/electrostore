using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Grpc;
using ElectrostoreAPI.Grpc.Services;
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Services.UserService;
using ElectrostoreAPI.Tests.Utils;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ElectrostoreAPI.Tests.Grpc;

public class ItemsHistoryGrpcServiceTests
{
    private readonly Mock<IItemHistoryService> _itemHistoryService = new();
    private readonly Mock<IUserService> _userService = new();
    private readonly Mock<ILogger<ItemsHistoryGrpcService>> _logger = new();

    private ItemsHistoryGrpcService CreateService() =>
        new(_itemHistoryService.Object, _userService.Object, _logger.Object);

    private static ReadExtendedItemHistoryDto BuildMovement(
        ReadItemDto? item = null,
        ReadUserDto? user = null) => new()
    {
        id_item_history = 1,
        id_item = item is null ? null : item.id_item,
        id_user = user?.id_user,
        type_item_history = ItemHistoryType.StockAdded,
        quantity_change_item_history = 5,
        old_quantity_item_history = 10,
        new_quantity_item_history = 15,
        notes_item_history = "restock",
        created_at = new DateTime(2026, 9, 1, 8, 30, 0, DateTimeKind.Utc),
        item = item,
        user = user
    };

    private static ReadItemDto BuildItem() => new()
    {
        id_item = 42,
        reference_name_item = "R10K",
        friendly_name_item = "Resistor 10k"
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
    public async Task GetItemsMovementReport_ShouldReturnMovementsAndAdministrators()
    {
        // Arrange
        var service = CreateService();
        var from = new DateTime(2026, 8, 28, 10, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 9, 4, 10, 0, 0, DateTimeKind.Utc);
        _itemHistoryService
            .Setup(s => s.GetItemsHistoryByPeriodAsync(from, to, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildMovement(BuildItem(), BuildAdmin())]);
        _userService
            .Setup(s => s.GetUsersByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildAdmin(), BuildAdmin(4)]);

        // Act
        var reply = await service.GetItemsMovementReport(new GetItemsMovementReportRequest
        {
            FromDate = from.ToString("o"),
            ToDate = to.ToString("o")
        }, TestServerCallContext.Create());

        // Assert
        var movement = Assert.Single(reply.Movements);
        Assert.Equal(42, movement.IdItem);
        Assert.Equal("Resistor 10k", movement.ItemName);
        Assert.Equal("StockAdded", movement.Type);
        Assert.Equal(5, movement.QuantityChange);
        Assert.Equal(10, movement.OldQuantity);
        Assert.Equal(15, movement.NewQuantity);
        Assert.Equal("Ada Lovelace", movement.UserName);
        Assert.Equal("restock", movement.Notes);
        Assert.Equal(2, reply.Recipients.Count);
        Assert.Equal("admin3@example.com", reply.Recipients[0].Email);
        Assert.Equal(from.ToString("o"), reply.FromDate);
        Assert.Equal(to.ToString("o"), reply.ToDate);
    }

    [Fact]
    public async Task GetItemsMovementReport_ShouldReturnEmptyStrings_WhenItemAndUserAreMissing()
    {
        // Arrange - a movement whose item or author has since been deleted
        var service = CreateService();
        _itemHistoryService
            .Setup(s => s.GetItemsHistoryByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([BuildMovement()]);
        _userService
            .Setup(s => s.GetUsersByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var reply = await service.GetItemsMovementReport(new GetItemsMovementReportRequest(), TestServerCallContext.Create());

        // Assert
        var movement = Assert.Single(reply.Movements);
        Assert.Equal(0, movement.IdItem);
        Assert.Equal(string.Empty, movement.ItemName);
        Assert.Equal(0, movement.IdUser);
        Assert.Equal(string.Empty, movement.UserName);
        Assert.Empty(reply.Recipients);
    }

    [Fact]
    public async Task GetItemsMovementReport_ShouldDefaultToTheLastSevenDays_WhenDatesAreMissing()
    {
        // Arrange
        var service = CreateService();
        DateTime? from = null;
        DateTime? to = null;
        _itemHistoryService
            .Setup(s => s.GetItemsHistoryByPeriodAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Callback<DateTime, DateTime, CancellationToken>((f, t, _) => { from = f; to = t; })
            .ReturnsAsync([]);
        _userService
            .Setup(s => s.GetUsersByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await service.GetItemsMovementReport(new GetItemsMovementReportRequest(), TestServerCallContext.Create());

        // Assert
        Assert.NotNull(from);
        Assert.NotNull(to);
        Assert.Equal(7, (to!.Value - from!.Value).Days);
    }

    [Fact]
    public async Task GetItemsMovementReport_ShouldThrowInvalidArgument_WhenDateIsNotIso8601()
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<RpcException>(() => service.GetItemsMovementReport(
            new GetItemsMovementReportRequest { FromDate = "last-monday" }, TestServerCallContext.Create()));

        // Assert
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }

    [Fact]
    public async Task GetItemsMovementReport_ShouldThrowInvalidArgument_WhenPeriodIsReversed()
    {
        // Arrange
        var service = CreateService();

        // Act
        var exception = await Assert.ThrowsAsync<RpcException>(() => service.GetItemsMovementReport(
            new GetItemsMovementReportRequest
            {
                FromDate = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc).ToString("o"),
                ToDate = new DateTime(2026, 8, 28, 0, 0, 0, DateTimeKind.Utc).ToString("o")
            }, TestServerCallContext.Create()));

        // Assert
        Assert.Equal(StatusCode.InvalidArgument, exception.StatusCode);
    }
}
