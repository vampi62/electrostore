using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.ItemHistoryService;

public class ItemHistoryService : IItemHistoryService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public ItemHistoryService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemHistoryDto>> GetItemHistoryByItemId(int itemId, int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsHistory.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_item", search_type = "eq", value = itemId.ToString() });
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_item_history", order = "desc" })
            .ToProjectedResponseAsync(
                h => new
                {
                    ItemHistory = h,
                    Item = expand != null && expand.Contains("item") ? h.Item : null,
                    Box = expand != null && expand.Contains("box") ? h.Box : null,
                    User = expand != null && expand.Contains("user") ? h.User : null
                },
                history => history.Select(h => {
                    return _mapper.Map<ReadExtendedItemHistoryDto>(h.ItemHistory) with
                    {
                        item = _mapper.Map<ReadExtendedItemDto>(h.Item),
                        box = _mapper.Map<ReadBoxDto>(h.Box),
                        user = _mapper.Map<ReadUserDto>(h.User),
                    };
                }).ToList());
    }

    public async Task<ReadExtendedItemHistoryDto> GetItemHistoryById(int id, int itemId, List<string>? expand = null)
    {
        var query = _context.ItemsHistory.AsQueryable();
        query = query.Where(h => h.id_item_history == id && h.id_item == itemId);
        var history = await query
            .Select(h => new
            {
                ItemHistory = h,
                Item = expand != null && expand.Contains("item") ? h.Item : null,
                Box = expand != null && expand.Contains("box") ? h.Box : null,
                User = expand != null && expand.Contains("user") ? h.User : null
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"ItemHistory with id '{id}' not found for Item with id '{itemId}'");
        return _mapper.Map<ReadExtendedItemHistoryDto>(history.ItemHistory) with
        {
            item = _mapper.Map<ReadExtendedItemDto>(history.Item),
            box = _mapper.Map<ReadBoxDto>(history.Box),
            user = _mapper.Map<ReadUserDto>(history.User),
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemHistoryDto>> GetItemsHistory(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var query = _context.ItemsHistory.AsQueryable();
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_item_history", order = "desc" })
            .ToProjectedResponseAsync(
                h => new
                {
                    ItemHistory = h,
                    Item = expand != null && expand.Contains("item") ? h.Item : null,
                    Box = expand != null && expand.Contains("box") ? h.Box : null,
                    User = expand != null && expand.Contains("user") ? h.User : null
                },
                history => history.Select(h => {
                    return _mapper.Map<ReadExtendedItemHistoryDto>(h.ItemHistory) with
                    {
                        item = _mapper.Map<ReadExtendedItemDto>(h.Item),
                        box = _mapper.Map<ReadBoxDto>(h.Box),
                        user = _mapper.Map<ReadUserDto>(h.User),
                    };
                }).ToList());
    }

    public async Task<IEnumerable<ReadExtendedItemHistoryDto>> GetItemsHistoryByPeriodAsync(DateTime fromDate, DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        var history = await _context.ItemsHistory
            .Where(h => h.created_at >= fromDate && h.created_at < toDate)
            .OrderBy(h => h.created_at)
            .Select(h => new
            {
                ItemHistory = h,
                Item = h.Item,
                User = h.User
            })
            .ToListAsync(cancellationToken);
        return history.Select(h => _mapper.Map<ReadExtendedItemHistoryDto>(h.ItemHistory) with
        {
            item = _mapper.Map<ReadExtendedItemDto>(h.Item),
            user = _mapper.Map<ReadUserDto>(h.User),
        }).ToList();
    }

    public async Task LogHistory(int? itemId, int? boxId, ItemHistoryType type,
        int? oldQuantity = null, int? newQuantity = null, string? notes = null)
    {
        var userId = _sessionService.GetClientId();

        int? quantityChange = null;
        if (oldQuantity.HasValue && newQuantity.HasValue)
        {
            quantityChange = newQuantity.Value - oldQuantity.Value;
        }
        else if (newQuantity.HasValue)
        {
            quantityChange = newQuantity.Value;
        }
        else if (oldQuantity.HasValue)
        {
            quantityChange = -oldQuantity.Value;
        }

        var entry = new ItemsHistory
        {
            id_item = itemId,
            id_box = boxId,
            id_user = userId,
            type_item_history = type,
            quantity_change_item_history = quantityChange,
            old_quantity_item_history = oldQuantity,
            new_quantity_item_history = newQuantity,
            notes_item_history = notes
        };
        _context.ItemsHistory.Add(entry);
        await _context.SaveChangesAsync();
    }
}
