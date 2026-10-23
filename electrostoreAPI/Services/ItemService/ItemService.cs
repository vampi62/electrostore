using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemHistoryService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.ItemService;

public class ItemService : IItemService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly IItemHistoryService _itemHistoryService;
    private readonly string _itemDocumentsPath = "itemDocuments";
    private readonly string _itemImagesPath = "itemImages";
    private readonly string _itemImagesThumbnailsPath = "itemImagesThumbnails";

    // List (not array): with C# 14 an array .Contains() in a LINQ query binds to MemoryExtensions.Contains(Span), which EF Core 9 cannot translate
    private static readonly List<ItemHistoryType> QuantityChangeHistoryTypes =
    [
        ItemHistoryType.StockAdded,
        ItemHistoryType.StockRemoved,
        ItemHistoryType.StockUpdated
    ];

    public ItemService(IMapper mapper, ApplicationDbContext context, IFileService fileService, IItemHistoryService itemHistoryService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
        _itemHistoryService = itemHistoryService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedItemDto>> GetItems(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var expandItemTags = expand?.Contains("item_tags") == true;
        var expandItemBoxs = expand?.Contains("item_boxs") == true;
        var expandCommandItems = expand?.Contains("command_items") == true;
        var expandProjectItems = expand?.Contains("project_items") == true;
        var expandItemDocuments = expand?.Contains("item_documents") == true;
        var expandItemHistory = expand?.Contains("item_history") == true;
        var query = _context.Items.AsQueryable();
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_item", order = "asc" })
            .ToProjectedResponseAsync(
                i => new
                {
                    Item = i,
                    ItemsTagsCount = i.ItemsTags.Count,
                    ItemsBoxsCount = i.ItemsBoxs.Count,
                    CommandsItemsCount = i.CommandsItems.Count,
                    ProjectsItemsCount = i.ProjectsItems.Count,
                    ItemsDocumentsCount = i.ItemsDocuments.Count,
                    ItemsTags = expandItemTags ? i.ItemsTags.Take(20).ToList() : null,
                    ItemsBoxs = expandItemBoxs ? i.ItemsBoxs.Take(20).ToList() : null,
                    CommandsItems = expandCommandItems ? i.CommandsItems.Take(20).ToList() : null,
                    ProjectsItems = expandProjectItems ? i.ProjectsItems.Take(20).ToList() : null,
                    ItemsDocuments = expandItemDocuments ? i.ItemsDocuments.Take(20).ToList() : null,
                    ItemsHistory = expandItemHistory ? i.ItemsHistory.OrderByDescending(h => h.created_at).Take(20).ToList() : null,
                    quantity_item = i.ItemsBoxs.Sum(ib => ib.quantity_item_box)
                },
                item => item.Select(i => {
                    return _mapper.Map<ReadExtendedItemDto>(i.Item) with
                    {
                        item_tags_count = i.ItemsTagsCount,
                        item_boxs_count = i.ItemsBoxsCount,
                        command_items_count = i.CommandsItemsCount,
                        project_items_count = i.ProjectsItemsCount,
                        item_documents_count = i.ItemsDocumentsCount,
                        item_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(i.ItemsTags),
                        item_boxs = _mapper.Map<IEnumerable<ReadItemBoxDto>>(i.ItemsBoxs),
                        command_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(i.CommandsItems),
                        project_items = _mapper.Map<IEnumerable<ReadProjectItemDto>>(i.ProjectsItems),
                        item_documents = _mapper.Map<IEnumerable<ReadItemDocumentDto>>(i.ItemsDocuments),
                        item_history = _mapper.Map<IEnumerable<ReadItemHistoryDto>>(i.ItemsHistory),
                        quantity_item = i.quantity_item
                    };
                }).ToList());
    }

    public async Task<ReadExtendedItemDto> GetItemById(int id, List<string>? expand = null)
    {
        var expandItemTags = expand?.Contains("item_tags") == true;
        var expandItemBoxs = expand?.Contains("item_boxs") == true;
        var expandCommandItems = expand?.Contains("command_items") == true;
        var expandProjectItems = expand?.Contains("project_items") == true;
        var expandItemDocuments = expand?.Contains("item_documents") == true;
        var expandItemHistory = expand?.Contains("item_history") == true;
        var query = _context.Items.AsQueryable();
        query = query.Where(i => i.id_item == id);
        var item = await query
            .Select(i => new
            {
                Item = i,
                ItemsTagsCount = i.ItemsTags.Count,
                ItemsBoxsCount = i.ItemsBoxs.Count,
                CommandsItemsCount = i.CommandsItems.Count,
                ProjectsItemsCount = i.ProjectsItems.Count,
                ItemsDocumentsCount = i.ItemsDocuments.Count,
                ItemsTags = expandItemTags ? i.ItemsTags.Take(20).ToList() : null,
                ItemsBoxs = expandItemBoxs ? i.ItemsBoxs.Take(20).ToList() : null,
                CommandsItems = expandCommandItems ? i.CommandsItems.Take(20).ToList() : null,
                ProjectsItems = expandProjectItems ? i.ProjectsItems.Take(20).ToList() : null,
                ItemsDocuments = expandItemDocuments ? i.ItemsDocuments.Take(20).ToList() : null,
                ItemsHistory = expandItemHistory ? i.ItemsHistory.OrderByDescending(h => h.created_at).Take(20).ToList() : null,
                quantity_item = i.ItemsBoxs.Sum(ib => ib.quantity_item_box)
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Item with id '{id}' not found");
        return _mapper.Map<ReadExtendedItemDto>(item.Item) with
        {
            item_tags_count = item.ItemsTagsCount,
            item_boxs_count = item.ItemsBoxsCount,
            command_items_count = item.CommandsItemsCount,
            project_items_count = item.ProjectsItemsCount,
            item_documents_count = item.ItemsDocumentsCount,
            item_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(item.ItemsTags),
            item_boxs = _mapper.Map<IEnumerable<ReadItemBoxDto>>(item.ItemsBoxs),
            command_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(item.CommandsItems),
            project_items = _mapper.Map<IEnumerable<ReadProjectItemDto>>(item.ProjectsItems),
            item_documents = _mapper.Map<IEnumerable<ReadItemDocumentDto>>(item.ItemsDocuments),
            item_history = _mapper.Map<IEnumerable<ReadItemHistoryDto>>(item.ItemsHistory),
            quantity_item = item.quantity_item
        };
    }

    public async Task<ReadItemDto> CreateItem(CreateItemDto itemDto)
    {
        // check if item already exists
        if (await _context.Items.AnyAsync(i => i.reference_name_item == itemDto.reference_name_item))
        {
            throw new InvalidOperationException($"Item with name '{itemDto.reference_name_item}' already exists");
        }
        var item = _mapper.Map<Items>(itemDto);
        _context.Items.Add(item);
        await _context.SaveChangesAsync();
        await _fileService.CreateDirectory(Path.Combine(_itemImagesPath, item.id_item.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_itemImagesThumbnailsPath, item.id_item.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_itemDocumentsPath, item.id_item.ToString()));
        if (itemDto.img_file is not null)
        {
            var (picturePath, thumbnailPath) = await _fileService.SaveImageWithThumbnail(Path.Combine(_itemImagesPath, item.id_item.ToString()), Path.Combine(_itemImagesThumbnailsPath, item.id_item.ToString()), itemDto.img_file);
            item.url_picture_item = picturePath;
            item.url_thumbnail_item = thumbnailPath;
            await _context.SaveChangesAsync();
        }
        await _itemHistoryService.LogHistory(item.id_item, null, ItemHistoryType.ItemCreated);
        return _mapper.Map<ReadItemDto>(item);
    }

    public async Task<ReadItemDto> UpdateItem(int id, UpdateItemDto itemDto)
    {
        var itemToUpdate = await _context.Items.FindAsync(id) ?? throw new KeyNotFoundException($"Item with id '{id}' not found");
        var hasChanges = false;
        if (itemDto.reference_name_item is not null)
        {
            // check if another item with the name already exists
            if (await _context.Items.AnyAsync(i => i.reference_name_item == itemDto.reference_name_item && i.id_item != id))
            {
                throw new InvalidOperationException($"Item with name '{itemDto.reference_name_item}' already exists");
            }
            if (itemToUpdate.reference_name_item != itemDto.reference_name_item)
            {
                itemToUpdate.reference_name_item = itemDto.reference_name_item;
                hasChanges = true;
            }
        }
        if (itemDto.friendly_name_item is not null && itemToUpdate.friendly_name_item != itemDto.friendly_name_item)
        {
            itemToUpdate.friendly_name_item = itemDto.friendly_name_item;
            hasChanges = true;
        }
        if (itemDto.threshold_min_item is not null && itemToUpdate.threshold_min_item != itemDto.threshold_min_item.Value)
        {
            itemToUpdate.threshold_min_item = itemDto.threshold_min_item.Value;
            hasChanges = true;
        }
        if (itemDto.description_item is not null && itemToUpdate.description_item != itemDto.description_item)
        {
            itemToUpdate.description_item = itemDto.description_item;
            hasChanges = true;
        }
        if (itemDto.unset_img_item is true)
        {
            hasChanges |= await _fileService.DeleteImageWithThumbnail(itemToUpdate.url_picture_item, itemToUpdate.url_thumbnail_item);
            itemToUpdate.url_picture_item = null;
            itemToUpdate.url_thumbnail_item = null;
        }
        else if (itemDto.img_file is not null)
        {
            await _fileService.DeleteImageWithThumbnail(itemToUpdate.url_picture_item, itemToUpdate.url_thumbnail_item);
            var (picturePath, thumbnailPath) = await _fileService.SaveImageWithThumbnail(Path.Combine(_itemImagesPath, id.ToString()), Path.Combine(_itemImagesThumbnailsPath, id.ToString()), itemDto.img_file);
            itemToUpdate.url_picture_item = picturePath;
            itemToUpdate.url_thumbnail_item = thumbnailPath;
            hasChanges = true;
        }
        await _context.SaveChangesAsync();
        if (hasChanges)
        {
            await _itemHistoryService.LogHistory(id, null, ItemHistoryType.ItemUpdated);
        }
        return _mapper.Map<ReadItemDto>(itemToUpdate);
    }

    public async Task DeleteItem(int id)
    {
        var itemToDelete = await _context.Items.FindAsync(id) ?? throw new KeyNotFoundException($"Item with id '{id}' not found");
        await _itemHistoryService.LogHistory(id, null, ItemHistoryType.ItemDeleted);
        _context.Items.Remove(itemToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_itemImagesPath, id.ToString()));
        await _fileService.DeleteDirectory(Path.Combine(_itemImagesThumbnailsPath, id.ToString()));
        await _fileService.DeleteDirectory(Path.Combine(_itemDocumentsPath, id.ToString()));
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<ReadItemDto>> GetLowStockItemsAsync(DateTime? sinceDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Items.Where(i => i.threshold_min_item > 0);
        if (sinceDate.HasValue)
        {
            query = query.Where(i => i.ItemsHistory.Any(h =>
                h.created_at >= sinceDate.Value && QuantityChangeHistoryTypes.Contains(h.type_item_history)));
        }
        var items = await query
            .Select(i => new
            {
                Item = i,
                quantity_item = i.ItemsBoxs.Sum(ib => ib.quantity_item_box)
            })
            .Where(x => x.quantity_item < x.Item.threshold_min_item)
            .ToListAsync(cancellationToken);
        return items.Select(x => _mapper.Map<ReadItemDto>(x.Item) with { quantity_item = x.quantity_item }).ToList();
    }
}