using System.Globalization;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Services.ItemHistoryService;
using ElectrostoreAPI.Services.UserService;
using Grpc.Core;

namespace ElectrostoreAPI.Grpc.Services;

public class ItemsHistoryGrpcService : ItemsHistoryGrpc.ItemsHistoryGrpcBase
{
    private readonly IItemHistoryService _itemHistoryService;
    private readonly IUserService _userService;
    private readonly ILogger<ItemsHistoryGrpcService> _logger;

    public ItemsHistoryGrpcService(
        IItemHistoryService itemHistoryService,
        IUserService userService,
        ILogger<ItemsHistoryGrpcService> logger)
    {
        _itemHistoryService = itemHistoryService;
        _userService = userService;
        _logger = logger;
    }

    public override async Task<GetItemsMovementReportReply> GetItemsMovementReport(
        GetItemsMovementReportRequest request, ServerCallContext context)
    {
        var toDate = ParseDate(request.ToDate) ?? DateTime.UtcNow;
        var fromDate = ParseDate(request.FromDate) ?? toDate.AddDays(-7);
        if (fromDate > toDate)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "from_date must be earlier than to_date"));
        }

        var movements = await _itemHistoryService.GetItemsHistoryByPeriodAsync(fromDate, toDate, context.CancellationToken);
        var admins = await _userService.GetUsersByRoleAsync(UserRole.Admin, context.CancellationToken);

        var reply = new GetItemsMovementReportReply
        {
            FromDate = fromDate.ToString("o"),
            ToDate = toDate.ToString("o")
        };
        reply.Movements.AddRange(movements.Select(m => new ItemMovementItem
        {
            IdItemHistory = m.id_item_history,
            IdItem = m.id_item ?? 0,
            ItemName = m.item?.friendly_name_item ?? string.Empty,
            Type = m.type_item_history.ToString(),
            QuantityChange = m.quantity_change_item_history ?? 0,
            OldQuantity = m.old_quantity_item_history ?? 0,
            NewQuantity = m.new_quantity_item_history ?? 0,
            IdUser = m.id_user ?? 0,
            UserName = m.user is null ? string.Empty : $"{m.user.firstname_user} {m.user.name_user}",
            Notes = m.notes_item_history ?? string.Empty,
            CreatedAt = m.created_at.ToString("o")
        }));
        reply.Recipients.AddRange(admins.Select(u => new ReportRecipientItem
        {
            IdUser = u.id_user,
            Email = u.email_user,
            Firstname = u.firstname_user,
            Name = u.name_user
        }));

        _logger.LogDebug(
            "GetItemsMovementReport: {MovementCount} movement(s) between {From} and {To} for {RecipientCount} recipient(s)",
            reply.Movements.Count, fromDate, toDate, reply.Recipients.Count);
        return reply;
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (!DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Invalid ISO 8601 date '{value}'"));
        }
        return parsed.Kind == DateTimeKind.Local ? parsed.ToUniversalTime() : parsed;
    }
}
