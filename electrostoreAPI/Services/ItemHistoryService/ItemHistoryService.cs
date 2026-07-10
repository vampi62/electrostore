using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
        var filterResult = default(Expression<Func<ItemsHistory, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_item", SearchType = "eq", Value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemsHistory>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemsHistory>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item_history", Order = "desc" };
                query = query.OrderByDescending(h => h.id_item_history);
            }
        }
        else
        {
            query = query.OrderByDescending(h => h.id_item_history);
        }
        query = query.Skip(offset).Take(limit);
        var history = await query
            .Select(h => new
            {
                ItemHistory = h,
                Item = expand != null && expand.Contains("item") ? h.Item : null,
                Box = expand != null && expand.Contains("box") ? h.Box : null,
                User = expand != null && expand.Contains("user") ? h.User : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemHistoryDto>
        {
            data = history.Select(h => {
                return _mapper.Map<ReadExtendedItemHistoryDto>(h.ItemHistory) with
                {
                    item = _mapper.Map<ReadExtendedItemDto>(h.Item),
                    box = _mapper.Map<ReadBoxDto>(h.Box),
                    user = _mapper.Map<ReadUserDto>(h.User),
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemsHistory.CountAsync(filterResult ?? (h => h.id_item == itemId)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsHistory.Skip(offset + limit).AnyAsync(filterResult ?? (h => h.id_item == itemId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
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
        var filterResult = default(Expression<Func<ItemsHistory, bool>>);
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemsHistory>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemsHistory>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item_history", Order = "desc" };
                query = query.OrderByDescending(h => h.id_item_history);
            }
        }
        else
        {
            query = query.OrderByDescending(h => h.id_item_history);
        }
        query = query.Skip(offset).Take(limit);
        var history = await query
            .Select(h => new
            {
                ItemHistory = h,
                Item = expand != null && expand.Contains("item") ? h.Item : null,
                Box = expand != null && expand.Contains("box") ? h.Box : null,
                User = expand != null && expand.Contains("user") ? h.User : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemHistoryDto>
        {
            data = history.Select(h => {
                return _mapper.Map<ReadExtendedItemHistoryDto>(h.ItemHistory) with
                {
                    item = _mapper.Map<ReadExtendedItemDto>(h.Item),
                    box = _mapper.Map<ReadBoxDto>(h.Box),
                    user = _mapper.Map<ReadUserDto>(h.User),
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemsHistory.CountAsync(filterResult ?? (h => true)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsHistory.Skip(offset + limit).AnyAsync(filterResult ?? (h => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
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
            type = type,
            quantity_change = quantityChange,
            old_quantity = oldQuantity,
            new_quantity = newQuantity,
            notes = notes
        };
        _context.ItemsHistory.Add(entry);
        await _context.SaveChangesAsync();
    }
}
