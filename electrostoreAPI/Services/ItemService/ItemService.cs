using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ItemService;

public class ItemService : IItemService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly FileService.FileService _fileService;
    private readonly string _itemDocumentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments");
    private readonly string _imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
    private readonly string _imagesThumbnailsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagesThumbnails");

    public ItemService(IMapper mapper, ApplicationDbContext context, FileService.FileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<IEnumerable<ReadExtendedItemDto>> GetItems(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Items.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(i => idResearch.Contains(i.id_item));
        }
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(i => i.id_item);
        var item = await query
            .Select(i => new
            {
                Item = i,
                ItemsTagsCount = i.ItemsTags.Count,
                ItemsBoxsCount = i.ItemsBoxs.Count,
                CommandsItemsCount = i.CommandsItems.Count,
                ProjetsItemsCount = i.ProjetsItems.Count,
                ItemsDocumentsCount = i.ItemsDocuments.Count,
                ItemsTags = expand != null && expand.Contains("item_tags") ? i.ItemsTags.Take(20).ToList() : null,
                ItemsBoxs = expand != null && expand.Contains("item_boxs") ? i.ItemsBoxs.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("command_items") ? i.CommandsItems.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("projet_items") ? i.ProjetsItems.Take(20).ToList() : null,
                ItemsDocuments = expand != null && expand.Contains("item_documents") ? i.ItemsDocuments.Take(20).ToList() : null
            })
            .ToListAsync();
        return item.Select(i => {
            return _mapper.Map<ReadExtendedItemDto>(i.Item) with
            {
                item_tags_count = i.ItemsTagsCount,
                item_boxs_count = i.ItemsBoxsCount,
                command_items_count = i.CommandsItemsCount,
                projet_items_count = i.ProjetsItemsCount,
                item_documents_count = i.ItemsDocumentsCount,
                item_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(i.ItemsTags),
                item_boxs = _mapper.Map<IEnumerable<ReadItemBoxDto>>(i.ItemsBoxs),
                command_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(i.CommandsItems),
                projet_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(i.ProjetsItems),
                item_documents = _mapper.Map<IEnumerable<ReadItemDocumentDto>>(i.ItemsDocuments)
            };
        }).ToList();
    }

    public async Task<int> GetItemsCount()
    {
        return await _context.Items.CountAsync();
    }

    public async Task<ReadExtendedItemDto> GetItemById(int id, List<string>? expand = null)
    {
        var query = _context.Items.AsQueryable();
        query = query.Where(i => i.id_item == id);
        var item = await query
            .Select(i => new
            {
                Item = i,
                ItemsTagsCount = i.ItemsTags.Count,
                ItemsBoxsCount = i.ItemsBoxs.Count,
                CommandsItemsCount = i.CommandsItems.Count,
                ProjetsItemsCount = i.ProjetsItems.Count,
                ItemsDocumentsCount = i.ItemsDocuments.Count,
                ItemsTags = expand != null && expand.Contains("item_tags") ? i.ItemsTags.Take(20).ToList() : null,
                ItemsBoxs = expand != null && expand.Contains("item_boxs") ? i.ItemsBoxs.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("command_items") ? i.CommandsItems.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("projet_items") ? i.ProjetsItems.Take(20).ToList() : null,
                ItemsDocuments = expand != null && expand.Contains("item_documents") ? i.ItemsDocuments.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Item with id {id} not found");
        return _mapper.Map<ReadExtendedItemDto>(item.Item) with
        {
            item_tags_count = item.ItemsTagsCount,
            item_boxs_count = item.ItemsBoxsCount,
            command_items_count = item.CommandsItemsCount,
            projet_items_count = item.ProjetsItemsCount,
            item_documents_count = item.ItemsDocumentsCount,
            item_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(item.ItemsTags),
            item_boxs = _mapper.Map<IEnumerable<ReadItemBoxDto>>(item.ItemsBoxs),
            command_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(item.CommandsItems),
            projet_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(item.ProjetsItems),
            item_documents = _mapper.Map<IEnumerable<ReadItemDocumentDto>>(item.ItemsDocuments)
        };
    }

    public async Task<ReadItemDto> CreateItem(CreateItemDto itemDto)
    {
        // check if img exists
        if (itemDto.id_img is not null && !await _context.Imgs.AnyAsync(i => i.id_img == itemDto.id_img))
        {
            throw new KeyNotFoundException($"Img with id {itemDto.id_img} not found");
        }
        // check if item already exists
        if (await _context.Items.AnyAsync(i => i.reference_name_item == itemDto.reference_name_item))
        {
            throw new InvalidOperationException($"Item with name {itemDto.reference_name_item} already exists");
        }
        var item = _mapper.Map<Items>(itemDto);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        await _fileService.CreateDirectory(Path.Combine(_imagesPath, item.id_item.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_imagesThumbnailsPath, item.id_item.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_itemDocumentsPath, item.id_item.ToString()));
        return _mapper.Map<ReadItemDto>(item);
    }

    public async Task<ReadItemDto> UpdateItem(int id, UpdateItemDto itemDto)
    {
        // check if img exists
        var itemToUpdate = await _context.Items.FindAsync(id) ?? throw new KeyNotFoundException($"Item with id {id} not found");
        if (itemDto.reference_name_item is not null)
        {
            // check if item already exists
            if (await _context.Items.AnyAsync(i => i.reference_name_item == itemDto.reference_name_item && i.id_item != id))
            {
                throw new InvalidOperationException($"Item with name {itemDto.reference_name_item} already exists");
            }
            itemToUpdate.reference_name_item = itemDto.reference_name_item;
        }
        if (itemDto.friendly_name_item is not null)
        {
            itemToUpdate.friendly_name_item = itemDto.friendly_name_item;
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
        await _fileService.DeleteDirectory(Path.Combine(_imagesPath, id.ToString()));
        await _fileService.DeleteDirectory(Path.Combine(_imagesThumbnailsPath, id.ToString()));
        await _fileService.DeleteDirectory(Path.Combine(_itemDocumentsPath, id.ToString()));
    }
}