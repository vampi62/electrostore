using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ItemDocumentService;

public class ItemDocumentService : IItemDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _itemDocumentsPath = "itemDocuments";
    private readonly ILogger<ItemDocumentService> _logger;

    public ItemDocumentService(IMapper mapper, ApplicationDbContext context, IFileService fileService, ILogger<ItemDocumentService> logger)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadItemDocumentDto>> GetItemsDocumentsByItemId(int itemId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        _logger.LogDebug("GetItemsDocumentsByItemId: itemId={ItemId}, limit={Limit}, offset={Offset}", itemId, limit, offset);
        // check if item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemId))
        {
            _logger.LogWarning("GetItemsDocumentsByItemId: Item {ItemId} not found", itemId);
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsDocuments.AsQueryable();
        var filterResult = default(Expression<Func<ItemsDocuments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_item", SearchType = "eq", Value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ItemsDocuments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ItemsDocuments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item_document", Order = "asc" };
                query = query.OrderBy(id => id.id_item_document);
            }
        }
        else
        {
            query = query.OrderBy(id => id.id_item_document);
        }
        query = query.Skip(offset).Take(limit);
        var itemDocument = await query.ToListAsync();
        return new PaginatedResponseDto<ReadItemDocumentDto>
        {
            data = _mapper.Map<List<ReadItemDocumentDto>>(itemDocument),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ItemsDocuments.CountAsync(filterResult ?? (id => id.id_item == itemId)),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsDocuments.Skip(offset + limit).AnyAsync(filterResult ?? (id => id.id_item == itemId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadItemDocumentDto> GetItemDocumentById(int id, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id);
        if (itemDocument is null)
        {
            _logger.LogWarning("GetItemDocumentById: ItemDocument {ItemDocumentId} not found", id);
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found");
        }
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            _logger.LogWarning("GetItemDocumentById: ItemDocument {ItemDocumentId} not found for Item {ItemId}", id, itemId);
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found for item with id '{itemId}'");
        }
        return _mapper.Map<ReadItemDocumentDto>(itemDocument);
    }

    public async Task<ReadItemDocumentDto> CreateItemDocument(CreateItemDocumentDto itemDocumentDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemDocumentDto.id_item))
        {
            _logger.LogWarning("CreateItemDocument: Item {ItemId} not found", itemDocumentDto.id_item);
            throw new KeyNotFoundException($"Item with id '{itemDocumentDto.id_item}' not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_itemDocumentsPath, itemDocumentDto.id_item.ToString()), itemDocumentDto.document.FileName, itemDocumentDto.document.ContentType, itemDocumentDto.document.OpenReadStream());
        var itemDocument = new ItemsDocuments
        {
            id_item = itemDocumentDto.id_item,
            url_item_document = savedFile.path,
            name_item_document = itemDocumentDto.name_item_document,
            type_item_document = savedFile.mimeType,
            size_item_document = itemDocumentDto.document.Length
        };
        await _context.ItemsDocuments.AddAsync(itemDocument);
        await _context.SaveChangesAsync();
        _logger.LogInformation("ItemDocument {ItemDocumentId} created for Item {ItemId}", itemDocument.id_item_document, itemDocument.id_item);
        return _mapper.Map<ReadItemDocumentDto>(itemDocument);
    }

    public async Task<ReadItemDocumentDto> UpdateItemDocument(int id, UpdateItemDocumentDto itemDocumentDto, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id);
        if (itemDocument is null)
        {
            _logger.LogWarning("UpdateItemDocument: ItemDocument {ItemDocumentId} not found", id);
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found");
        }
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            _logger.LogWarning("UpdateItemDocument: ItemDocument {ItemDocumentId} not found for Item {ItemId}", id, itemId);
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found for item with id '{itemId}'");
        }
        if (itemDocumentDto.name_item_document is not null)
        {
            itemDocument.name_item_document = itemDocumentDto.name_item_document;
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("ItemDocument {ItemDocumentId} updated", id);
        return _mapper.Map<ReadItemDocumentDto>(itemDocument);
    }

    public async Task DeleteItemDocument(int id, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id);
        if (itemDocument is null)
        {
            _logger.LogWarning("DeleteItemDocument: ItemDocument {ItemDocumentId} not found", id);
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found");
        }
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            _logger.LogWarning("DeleteItemDocument: ItemDocument {ItemDocumentId} not found for Item {ItemId}", id, itemId);
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found for item with id '{itemId}'");
        }
        await _fileService.DeleteFile(itemDocument.url_item_document);
        _context.ItemsDocuments.Remove(itemDocument);
        await _context.SaveChangesAsync();
        _logger.LogInformation("ItemDocument {ItemDocumentId} deleted", id);
    }
}