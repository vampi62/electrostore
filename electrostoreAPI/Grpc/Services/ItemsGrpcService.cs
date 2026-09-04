using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Services.ItemService;
using ElectrostoreAPI.Services.UserService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class ItemsGrpcService : ItemsGrpc.ItemsGrpcBase
{
    private readonly IItemService _itemService;
    private readonly IUserService _userService;
    private readonly ILogger<ItemsGrpcService> _logger;

    public ItemsGrpcService(
        IItemService itemService,
        IUserService userService,
        ILogger<ItemsGrpcService> logger)
    {
        _itemService = itemService;
        _userService = userService;
        _logger = logger;
    }

    public override async Task<GetLowStockItemsReply> GetLowStockItems(
        GetLowStockItemsRequest request, ServerCallContext context)
    {
        var lowStockItems = await _itemService.GetLowStockItemsAsync(context.CancellationToken);
        var admins = await _userService.GetUsersByRoleAsync(UserRole.Admin, context.CancellationToken);

        var reply = new GetLowStockItemsReply();
        reply.Items.AddRange(lowStockItems.Select(i => new LowStockItemItem
        {
            IdItem = i.id_item,
            ReferenceNameItem = i.reference_name_item,
            FriendlyNameItem = i.friendly_name_item,
            QuantityItem = i.quantity_item,
            ThresholdMinItem = i.threshold_min_item
        }));
        reply.Recipients.AddRange(admins.Select(u => new LowStockRecipientItem
        {
            IdUser = u.id_user,
            Email = u.email_user,
            Firstname = u.firstname_user,
            Name = u.name_user
        }));

        _logger.LogDebug(
            "GetLowStockItems: {ItemCount} item(s) below threshold for {RecipientCount} recipient(s)",
            reply.Items.Count, reply.Recipients.Count);
        return reply;
    }
}
