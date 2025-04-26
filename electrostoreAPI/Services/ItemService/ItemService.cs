using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ItemService;

public class ItemService : IItemService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ItemService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedItemDto>> GetItems(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Items.AsQueryable();
        if (idResearch != null)
        {
            query = query.Where(b => idResearch.Contains(b.id_item));
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("item_tags"))
        {
            query = query.Include(i => i.ItemsTags);
        }
        if (expand != null && expand.Contains("item_boxs"))
        {
            query = query.Include(i => i.ItemsBoxs);
        }
        if (expand != null && expand.Contains("command_items"))
        {
            query = query.Include(i => i.CommandsItems);
        }
        if (expand != null && expand.Contains("projet_items"))
        {
            query = query.Include(i => i.ProjetsItems);
        }
        if (expand != null && expand.Contains("item_documents"))
        {
            query = query.Include(i => i.ItemsDocuments);
        }
        var item = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedItemDto>>(item);
    }

    public async Task<int> GetItemsCount()
    {
        return await _context.Items.CountAsync();
    }

    public async Task<ReadExtendedItemDto> GetItemById(int id, List<string>? expand = null)
    {
        var query = _context.Items.AsQueryable();
        query = query.Where(i => i.id_item == id);
        if (expand != null && expand.Contains("item_tags"))
        {
            query = query.Include(i => i.ItemsTags);
        }
        if (expand != null && expand.Contains("item_boxs"))
        {
            query = query.Include(i => i.ItemsBoxs);
        }
        if (expand != null && expand.Contains("command_items"))
        {
            query = query.Include(i => i.CommandsItems);
        }
        if (expand != null && expand.Contains("projet_items"))
        {
            query = query.Include(i => i.ProjetsItems);
        }
        if (expand != null && expand.Contains("item_documents"))
        {
            query = query.Include(i => i.ItemsDocuments);
        }
        var item = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Item with id {id} not found");
        return _mapper.Map<ReadExtendedItemDto>(item);
    }

    public async Task<ReadItemDto> CreateItem(CreateItemDto itemDto)
    {
        // check if img exists
        if (itemDto.id_img is not null && !await _context.Imgs.AnyAsync(i => i.id_img == itemDto.id_img))
        {
            throw new KeyNotFoundException($"Img with id {itemDto.id_img} not found");
        }
        // check if item already exists
        if (await _context.Items.AnyAsync(i => i.nom_item == itemDto.nom_item))
        {
            throw new InvalidOperationException($"Item with name {itemDto.nom_item} already exists");
        }
        var item = new Items
        {
            id_img = itemDto.id_img,
            nom_item = itemDto.nom_item,
            seuil_min_item = itemDto.seuil_min_item,
            description_item = itemDto.description_item,
        };
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", item.id_item.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", item.id_item.ToString()));
        }
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", item.id_item.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", item.id_item.ToString()));
        }
        return _mapper.Map<ReadItemDto>(item);
    }

    public async Task<ReadItemDto> UpdateItem(int id, UpdateItemDto itemDto)
    {
        // check if img exists
        var itemToUpdate = await _context.Items.FindAsync(id) ?? throw new KeyNotFoundException($"Item with id {id} not found");
        if (itemDto.nom_item is not null)
        {
            // check if item already exists
            if (await _context.Items.AnyAsync(i => i.nom_item == itemDto.nom_item))
            {
                throw new InvalidOperationException($"Item with name {itemDto.nom_item} already exists");
            }
            itemToUpdate.nom_item = itemDto.nom_item;
        }
        if (itemDto.seuil_min_item is not null)
        {
            itemToUpdate.seuil_min_item = itemDto.seuil_min_item.Value;
        }
        if (itemDto.description_item is not null)
        {
            itemToUpdate.description_item = itemDto.description_item;
        }
        if (itemDto.id_img is not null)
        {
            var img = await _context.Imgs.FindAsync(itemDto.id_img);
            if ((img is null) || (id != img.id_item))
            {
                throw new KeyNotFoundException($"Img with id {itemDto.id_img} not found");
            }
            itemToUpdate.id_img = itemDto.id_img;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemDto>(itemToUpdate);
    }

    public async Task DeleteItem(int id)
    {
        var itemToDelete = await _context.Items.FindAsync(id) ?? throw new KeyNotFoundException($"Item with id {id} not found");
        _context.Items.Remove(itemToDelete);
        await _context.SaveChangesAsync();
        //remove folder in wwwroot/images
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", id.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", id.ToString()), true);
        }
        //remove folder in wwwroot/itemDocuments
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", id.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", id.ToString()), true);
        }
    }
}