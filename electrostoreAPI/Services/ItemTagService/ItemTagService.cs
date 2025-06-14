using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

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

    public async Task<IEnumerable<ReadExtendedItemTagDto>> GetItemsTagsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        var query = _context.ItemsTags.AsQueryable();
        query = query.Where(it => it.id_item == itemId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(it => it.id_tag);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(it => it.Tag);
        }
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(it => it.Item);
        }
        var itemTag = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedItemTagDto>>(itemTag);
    }

    public async Task<int> GetItemsTagsCountByItemId(int itemId)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        return await _context.ItemsTags
            .Where(it => it.id_item == itemId)
            .CountAsync();
    }

    public async Task<IEnumerable<ReadExtendedItemTagDto>> GetItemsTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        var query = _context.ItemsTags.AsQueryable();
        query = query.Where(it => it.id_tag == tagId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(it => it.id_item);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(it => it.Tag);
        }
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(it => it.Item);
        }
        var itemTag = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedItemTagDto>>(itemTag);
    }

    public async Task<int> GetItemsTagsCountByTagId(int tagId)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        return await _context.ItemsTags
            .Where(it => it.id_tag == tagId)
            .CountAsync();
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
        var itemTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ItemTag with id_item {itemId} and id_tag {tagId} not found");
        return _mapper.Map<ReadExtendedItemTagDto>(itemTag);
    }

    public async Task<ReadItemTagDto> CreateItemTag(CreateItemTagDto itemTagDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemTagDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id {itemTagDto.id_item} not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == itemTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id {itemTagDto.id_tag} not found");
        }
        // check if itemTag already exists
        if (await _context.ItemsTags.AnyAsync(it => it.id_item == itemTagDto.id_item && it.id_tag == itemTagDto.id_tag))
        {
            throw new InvalidOperationException($"ItemTag with id_item {itemTagDto.id_item} and id_tag {itemTagDto.id_tag} already exists");
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
        var itemTagToDelete = await _context.ItemsTags.FindAsync(itemId, tagId) ?? throw new KeyNotFoundException($"ItemTag with id_item {itemId} and id_tag {tagId} not found");
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