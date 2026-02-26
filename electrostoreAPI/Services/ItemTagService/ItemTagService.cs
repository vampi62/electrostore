using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Extensions;
using electrostore.Models;
using System.Linq.Expressions;

namespace electrostore.Services.ItemTagService;

public class ItemTagService : IItemTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ItemTagService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemTagDto>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsTags.AsQueryable();
        var filterResult = default(Expression<Func<ItemsTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_item", SearchType = "eq", Value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemsTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemsTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_tag", Order = "asc" };
                query = query.OrderBy(it => it.id_tag);
            }
        }
        else
        {
            query = query.OrderBy(it => it.id_tag);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(it => it.Tag);
        }
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(it => it.Item);
        }
        var itemTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemTagDto>
        {
            data = _mapper.Map<List<ReadExtendedItemTagDto>>(itemTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemsTags.CountAsync(filterResult ?? (it => it.id_item == itemId)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsTags.Skip(offset + limit).AnyAsync(filterResult ?? (it => it.id_item == itemId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemTagDto>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id '{tagId}' not found");
        }
        var query = _context.ItemsTags.AsQueryable();
        var filterResult = default(Expression<Func<ItemsTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_tag", SearchType = "eq", Value = tagId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemsTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemsTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item", Order = "asc" };
                query = query.OrderBy(it => it.id_item);
            }
        }
        else
        {
            query = query.OrderBy(it => it.id_item);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(it => it.Tag);
        }
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(it => it.Item);
        }
        var itemTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemTagDto>
        {
            data = _mapper.Map<List<ReadExtendedItemTagDto>>(itemTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemsTags.CountAsync(filterResult ?? (it => it.id_tag == tagId)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsTags.Skip(offset + limit).AnyAsync(filterResult ?? (it => it.id_tag == tagId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedItemTagDto> GetItemTagById(int itemId, int tagId, List<string>? expand = null)
    {
        var query = _context.ItemsTags.AsQueryable();
        query = query.Where(it => it.id_item == itemId && it.id_tag == tagId);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(it => it.Tag);
        }
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(it => it.Item);
        }
        var itemTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ItemTag with id_item '{itemId}' and id_tag '{tagId}' not found");
        return _mapper.Map<ReadExtendedItemTagDto>(itemTag);
    }

    public async Task<ReadItemTagDto> CreateItemTag(CreateItemTagDto itemTagDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemTagDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{itemTagDto.id_item}' not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == itemTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id '{itemTagDto.id_tag}' not found");
        }
        // check if itemTag already exists
        if (await _context.ItemsTags.AnyAsync(it => it.id_item == itemTagDto.id_item && it.id_tag == itemTagDto.id_tag))
        {
            throw new InvalidOperationException($"ItemTag with id_item '{itemTagDto.id_item}' and id_tag '{itemTagDto.id_tag}' already exists");
        }
        var itemTag = _mapper.Map<ItemsTags>(itemTagDto);
        _context.ItemsTags.Add(itemTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemTagDto>(itemTag);
    }

    public async Task<ReadBulkItemTagDto> CreateBulkItemTag(List<CreateItemTagDto> itemTagBulkDto)
    {
        var validQuery = new List<ReadItemTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var itemTagDto in itemTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateItemTag(itemTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = itemTagDto
                });
            }
        }
        return new ReadBulkItemTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteItemTag(int itemId, int tagId)
    {
        var itemTagToDelete = await _context.ItemsTags.FindAsync(itemId, tagId) ?? throw new KeyNotFoundException($"ItemTag with id_item '{itemId}' and id_tag '{tagId}' not found");
        _context.ItemsTags.Remove(itemTagToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkItemTagDto> DeleteBulkItemTag(List<CreateItemTagDto> itemTagBulkDto)
    {
        var validQuery = new List<ReadItemTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var itemTagDto in itemTagBulkDto)
        {
            try
            {
                await DeleteItemTag(itemTagDto.id_item, itemTagDto.id_tag);
                validQuery.Add(new ReadItemTagDto
                {
                    id_item = itemTagDto.id_item,
                    id_tag = itemTagDto.id_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = itemTagDto
                });
            }
        }
        return new ReadBulkItemTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}