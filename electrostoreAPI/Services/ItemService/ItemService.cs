using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ItemHistoryService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ItemService;

public class ItemService : IItemService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly IItemHistoryService _itemHistoryService;
    private readonly string _itemDocumentsPath = "itemDocuments";
    private readonly string _imagesPath = "images";
    private readonly string _imagesThumbnailsPath = "imagesThumbnails";

    private static readonly ItemHistoryType[] QuantityChangeHistoryTypes =
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
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Items.AsQueryable();
        var filterResult = default(Expression<Func<Items, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(i => idResearch.Contains(i.id_item));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Items>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Items>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { field = "id_item", order = "asc" };
                    query = query.OrderBy(i => i.id_item);
                }
            }
            else
            {
                query = query.OrderBy(i => i.id_item);
            }
        }
        query = query.Skip(offset).Take(limit);
        var item = await query
            .Select(i => new
            {
                Item = i,
                ItemsTagsCount = i.ItemsTags.Count,
                ItemsBoxsCount = i.ItemsBoxs.Count,
                CommandsItemsCount = i.CommandsItems.Count,
                ProjectsItemsCount = i.ProjectsItems.Count,
                ItemsDocumentsCount = i.ItemsDocuments.Count,
                ItemsTags = expand != null && expand.Contains("item_tags") ? i.ItemsTags.Take(20).ToList() : null,
                ItemsBoxs = expand != null && expand.Contains("item_boxs") ? i.ItemsBoxs.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("command_items") ? i.CommandsItems.Take(20).ToList() : null,
                ProjectsItems = expand != null && expand.Contains("project_items") ? i.ProjectsItems.Take(20).ToList() : null,
                ItemsDocuments = expand != null && expand.Contains("item_documents") ? i.ItemsDocuments.Take(20).ToList() : null,
                ItemsHistory = expand != null && expand.Contains("item_history") ? i.ItemsHistory.OrderByDescending(h => h.created_at).Take(20).ToList() : null,
                quantity_item = i.ItemsBoxs.Sum(ib => ib.quantity_item_box)
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedItemDto>
        {
            data = item.Select(i => {
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
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Items.CountAsync(filterResult ?? (i => true)),
                next_offset = offset + limit,
                has_more = await _context.Items.Skip(offset + limit).AnyAsync(filterResult ?? (i => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
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
                ProjectsItemsCount = i.ProjectsItems.Count,
                ItemsDocumentsCount = i.ItemsDocuments.Count,
                ItemsTags = expand != null && expand.Contains("item_tags") ? i.ItemsTags.Take(20).ToList() : null,
                ItemsBoxs = expand != null && expand.Contains("item_boxs") ? i.ItemsBoxs.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("command_items") ? i.CommandsItems.Take(20).ToList() : null,
                ProjectsItems = expand != null && expand.Contains("project_items") ? i.ProjectsItems.Take(20).ToList() : null,
                ItemsDocuments = expand != null && expand.Contains("item_documents") ? i.ItemsDocuments.Take(20).ToList() : null,
                ItemsHistory = expand != null && expand.Contains("item_history") ? i.ItemsHistory.OrderByDescending(h => h.created_at).Take(20).ToList() : null,
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
        await _fileService.CreateDirectory(Path.Combine(_imagesPath, item.id_item.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_imagesThumbnailsPath, item.id_item.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_itemDocumentsPath, item.id_item.ToString()));
        if (itemDto.img_file is not null)
        {
            var savedImg = await _fileService.SaveFile(Path.Combine(_imagesPath, item.id_item.ToString()), itemDto.img_file.FileName, itemDto.img_file.ContentType, itemDto.img_file.OpenReadStream());
            var savedThumbnail = await _fileService.GenerateThumbnail(
                savedImg.path,
                Path.Combine(_imagesThumbnailsPath, item.id_item.ToString()),
                256, 256);
            item.url_picture_item = savedImg.path;
            item.url_thumbnail_item = savedThumbnail.path;
            await _context.SaveChangesAsync();
        }
        await _itemHistoryService.LogHistory(item.id_item, null, ItemHistoryType.ItemCreated);
        return _mapper.Map<ReadItemDto>(item);
    }

    public async Task<ReadItemDto> UpdateItem(int id, UpdateItemDto itemDto)
    {
        var itemToUpdate = await _context.Items.FindAsync(id) ?? throw new KeyNotFoundException($"Item with id '{id}' not found");
        if (itemDto.reference_name_item is not null)
        {
            // check if another item with the name already exists
            if (await _context.Items.AnyAsync(i => i.reference_name_item == itemDto.reference_name_item && i.id_item != id))
            {
                throw new InvalidOperationException($"Item with name '{itemDto.reference_name_item}' already exists");
            }
            itemToUpdate.reference_name_item = itemDto.reference_name_item;
        }
        if (itemDto.friendly_name_item is not null)
        {
            itemToUpdate.friendly_name_item = itemDto.friendly_name_item;
        }
        if (itemDto.threshold_min_item is not null)
        {
            itemToUpdate.threshold_min_item = itemDto.threshold_min_item.Value;
        }
        if (itemDto.description_item is not null)
        {
            itemToUpdate.description_item = itemDto.description_item;
        }
        if (itemDto.unset_img_item is true)
        {
            if (itemToUpdate.url_picture_item is not null)
            {
                await _fileService.DeleteFile(itemToUpdate.url_picture_item);
            }
            if (itemToUpdate.url_thumbnail_item is not null)
            {
                await _fileService.DeleteFile(itemToUpdate.url_thumbnail_item);
            }
            itemToUpdate.url_picture_item = null;
            itemToUpdate.url_thumbnail_item = null;
        }
        else if (itemDto.img_file is not null)
        {
            if (itemToUpdate.url_picture_item is not null)
            {
                await _fileService.DeleteFile(itemToUpdate.url_picture_item);
            }
            if (itemToUpdate.url_thumbnail_item is not null)
            {
                await _fileService.DeleteFile(itemToUpdate.url_thumbnail_item);
            }
            var savedImg = await _fileService.SaveFile(Path.Combine(_imagesPath, id.ToString()), itemDto.img_file.FileName, itemDto.img_file.ContentType, itemDto.img_file.OpenReadStream());
            var savedThumbnail = await _fileService.GenerateThumbnail(
                savedImg.path,
                Path.Combine(_imagesThumbnailsPath, id.ToString()),
                256, 256);
            itemToUpdate.url_picture_item = savedImg.path;
            itemToUpdate.url_thumbnail_item = savedThumbnail.path;
        }
        await _context.SaveChangesAsync();
        await _itemHistoryService.LogHistory(id, null, ItemHistoryType.ItemUpdated);
        return _mapper.Map<ReadItemDto>(itemToUpdate);
    }

    public async Task DeleteItem(int id)
    {
        var itemToDelete = await _context.Items.FindAsync(id) ?? throw new KeyNotFoundException($"Item with id '{id}' not found");
        await _itemHistoryService.LogHistory(id, null, ItemHistoryType.ItemDeleted);
        _context.Items.Remove(itemToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_imagesPath, id.ToString()));
        await _fileService.DeleteDirectory(Path.Combine(_imagesThumbnailsPath, id.ToString()));
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