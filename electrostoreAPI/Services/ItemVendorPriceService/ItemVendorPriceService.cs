using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ItemVendorPriceService;

public class ItemVendorPriceService : IItemVendorPriceService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ItemVendorPriceService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemVendorPriceDto>> GetPriceHistoryByItemVendorId(int itemVendorId, int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        if (!await _context.ItemsVendors.AnyAsync(iv => iv.id_item_vendor == itemVendorId))
        {
            throw new KeyNotFoundException($"ItemVendor with id '{itemVendorId}' not found");
        }
        var query = _context.ItemVendorPrices.AsQueryable();
        var filterResult = default(Expression<Func<ItemVendorPrices, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_item_vendor", SearchType = "eq", Value = itemVendorId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemVendorPrices>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemVendorPrices>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item_vendor_price", Order = "desc" };
                query = query.OrderByDescending(p => p.id_item_vendor_price);
            }
        }
        else
        {
            query = query.OrderByDescending(p => p.id_item_vendor_price);
        }
        query = query.Skip(offset).Take(limit);
        var prices = await query
            .Select(p => new
            {
                Price = p,
                ItemVendor = expand != null && expand.Contains("item_vendor") ? p.ItemVendor : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemVendorPriceDto>
        {
            data = prices.Select(p => {
                return _mapper.Map<ReadExtendedItemVendorPriceDto>(p.Price) with
                {
                    item_vendor = _mapper.Map<ReadItemVendorDto>(p.ItemVendor),
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemVendorPrices.CountAsync(filterResult ?? (p => p.id_item_vendor == itemVendorId)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemVendorPrices.Skip(offset + limit).AnyAsync(filterResult ?? (p => p.id_item_vendor == itemVendorId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedItemVendorPriceDto> GetPriceHistoryById(int id, int? itemVendorId = null, List<string>? expand = null)
    {
        var query = _context.ItemVendorPrices.AsQueryable();
        query = query.Where(p => p.id_item_vendor_price == id);
        if (itemVendorId is not null)
        {
            query = query.Where(p => p.id_item_vendor == itemVendorId);
        }
        var price = await query
            .Select(p => new
            {
                Price = p,
                ItemVendor = expand != null && expand.Contains("item_vendor") ? p.ItemVendor : null
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"ItemVendorPrice with id '{id}' not found");
        return _mapper.Map<ReadExtendedItemVendorPriceDto>(price.Price) with
        {
            item_vendor = _mapper.Map<ReadItemVendorDto>(price.ItemVendor),
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemVendorPriceDto>> GetPriceHistory(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var query = _context.ItemVendorPrices.AsQueryable();
        var filterResult = default(Expression<Func<ItemVendorPrices, bool>>);
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemVendorPrices>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemVendorPrices>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item_vendor_price", Order = "desc" };
                query = query.OrderByDescending(p => p.id_item_vendor_price);
            }
        }
        else
        {
            query = query.OrderByDescending(p => p.id_item_vendor_price);
        }
        query = query.Skip(offset).Take(limit);
        var prices = await query
            .Select(p => new
            {
                Price = p,
                ItemVendor = expand != null && expand.Contains("item_vendor") ? p.ItemVendor : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemVendorPriceDto>
        {
            data = prices.Select(p => {
                return _mapper.Map<ReadExtendedItemVendorPriceDto>(p.Price) with
                {
                    item_vendor = _mapper.Map<ReadItemVendorDto>(p.ItemVendor),
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemVendorPrices.CountAsync(filterResult ?? (p => true)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemVendorPrices.Skip(offset + limit).AnyAsync(filterResult ?? (p => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadItemVendorPriceDto> RecordPriceObservation(int idItemVendor, float price, string currency,
        int quantity = 1, string? priceBreaksJson = null)
    {
        if (!await _context.ItemsVendors.AnyAsync(iv => iv.id_item_vendor == idItemVendor))
        {
            throw new KeyNotFoundException($"ItemVendor with id '{idItemVendor}' not found");
        }
        var entry = new ItemVendorPrices
        {
            id_item_vendor = idItemVendor,
            price_item_vendor_price = price,
            currency_item_vendor_price = currency,
            quantity_item_vendor_price = quantity,
            price_breaks_item_vendor_price = priceBreaksJson
        };
        _context.ItemVendorPrices.Add(entry);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemVendorPriceDto>(entry);
    }
}
