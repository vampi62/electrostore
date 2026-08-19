using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ItemVendorService;

public class ItemVendorService : IItemVendorService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ItemVendorService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemVendorDto>> GetItemVendorsByItemId(int itemId, int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsVendors.AsQueryable();
        var filterResult = default(Expression<Func<ItemsVendors, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_item", SearchType = "eq", Value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemsVendors>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemsVendors>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item_vendor", Order = "asc" };
                query = query.OrderBy(iv => iv.id_item_vendor);
            }
        }
        else
        {
            query = query.OrderBy(iv => iv.id_item_vendor);
        }
        query = query.Skip(offset).Take(limit);
        var itemVendors = await query
            .Select(iv => new
            {
                ItemVendor = iv,
                Item = expand != null && expand.Contains("item") ? iv.Item : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemVendorDto>
        {
            data = itemVendors.Select(iv => {
                return _mapper.Map<ReadExtendedItemVendorDto>(iv.ItemVendor) with
                {
                    item = _mapper.Map<ReadExtendedItemDto>(iv.Item),
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemsVendors.CountAsync(filterResult ?? (iv => iv.id_item == itemId)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsVendors.Skip(offset + limit).AnyAsync(filterResult ?? (iv => iv.id_item == itemId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedItemVendorDto> GetItemVendorById(int id, int? itemId = null, List<string>? expand = null)
    {
        var query = _context.ItemsVendors.AsQueryable();
        query = query.Where(iv => iv.id_item_vendor == id);
        if (itemId is not null)
        {
            query = query.Where(iv => iv.id_item == itemId);
        }
        var itemVendor = await query
            .Select(iv => new
            {
                ItemVendor = iv,
                Item = expand != null && expand.Contains("item") ? iv.Item : null
            })
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"ItemVendor with id '{id}' not found");
        return _mapper.Map<ReadExtendedItemVendorDto>(itemVendor.ItemVendor) with
        {
            item = _mapper.Map<ReadExtendedItemDto>(itemVendor.Item),
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemVendorDto>> GetItemVendors(int limit = 100, int offset = 0,
        List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var query = _context.ItemsVendors.AsQueryable();
        var filterResult = default(Expression<Func<ItemsVendors, bool>>);
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemsVendors>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemsVendors>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item_vendor", Order = "asc" };
                query = query.OrderBy(iv => iv.id_item_vendor);
            }
        }
        else
        {
            query = query.OrderBy(iv => iv.id_item_vendor);
        }
        query = query.Skip(offset).Take(limit);
        var itemVendors = await query
            .Select(iv => new
            {
                ItemVendor = iv,
                Item = expand != null && expand.Contains("item") ? iv.Item : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemVendorDto>
        {
            data = itemVendors.Select(iv => {
                return _mapper.Map<ReadExtendedItemVendorDto>(iv.ItemVendor) with
                {
                    item = _mapper.Map<ReadExtendedItemDto>(iv.Item),
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemsVendors.CountAsync(filterResult ?? (iv => true)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsVendors.Skip(offset + limit).AnyAsync(filterResult ?? (iv => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadItemVendorDto> CreateItemVendor(CreateItemVendorDto itemVendorDto)
    {
        if (!await _context.Items.AnyAsync(i => i.id_item == itemVendorDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{itemVendorDto.id_item}' not found");
        }
        var itemVendor = _mapper.Map<ItemsVendors>(itemVendorDto);
        await _context.ItemsVendors.AddAsync(itemVendor);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemVendorDto>(itemVendor);
    }

    public async Task<ReadItemVendorDto> UpdateItemVendor(int id, UpdateItemVendorDto itemVendorDto, int? itemId = null)
    {
        var itemVendor = await _context.ItemsVendors.FindAsync(id) ?? throw new KeyNotFoundException($"ItemVendor with id '{id}' not found");
        if (itemId is not null && itemVendor.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemVendor with id '{id}' not found for item with id '{itemId}'");
        }
        if (itemVendorDto.vendor_type_item_vendor is not null)
        {
            itemVendor.vendor_type_item_vendor = itemVendorDto.vendor_type_item_vendor.Value;
        }
        if (itemVendorDto.vendor_sku_item_vendor is not null)
        {
            itemVendor.vendor_sku_item_vendor = itemVendorDto.vendor_sku_item_vendor;
        }
        if (itemVendorDto.url_item_vendor is not null)
        {
            itemVendor.url_item_vendor = itemVendorDto.url_item_vendor;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemVendorDto>(itemVendor);
    }

    public async Task DeleteItemVendor(int id, int? itemId = null)
    {
        var itemVendor = await _context.ItemsVendors.FindAsync(id) ?? throw new KeyNotFoundException($"ItemVendor with id '{id}' not found");
        if (itemId is not null && itemVendor.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemVendor with id '{id}' not found for item with id '{itemId}'");
        }
        _context.ItemsVendors.Remove(itemVendor);
        await _context.SaveChangesAsync();
    }
}
