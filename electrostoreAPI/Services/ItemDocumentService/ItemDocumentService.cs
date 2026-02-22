using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Extensions;
using electrostore.Models;
using electrostore.Services.FileService;

namespace electrostore.Services.ItemDocumentService;

public class ItemDocumentService : IItemDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _itemDocumentsPath = "itemDocuments";

    public ItemDocumentService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadItemDocumentDto>> GetItemsDocumentsByItemId(int itemId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsDocuments.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_item", SearchType = "eq", Value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            var filterResult = RsqlParserExtensions.ToFilterExpression<ItemsDocuments>(rsql);
            query = query.Where(filterResult.Item1);
            rsql = filterResult.Item2;
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
                total = await _context.ItemsDocuments.Where(id => id.id_item == itemId).CountAsync(),
                nextOffset = offset + limit,
                hasMore = await _context.ItemsDocuments.Where(id => id.id_item == itemId).Skip(offset + limit).AnyAsync()
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadItemDocumentDto> GetItemDocumentById(int id, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ItemDocument with id '{id}' not found");
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found for item with id '{itemId}'");
        }
        return _mapper.Map<ReadItemDocumentDto>(itemDocument);
    }

    public async Task<ReadItemDocumentDto> CreateItemDocument(CreateItemDocumentDto itemDocumentDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemDocumentDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{itemDocumentDto.id_item}' not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_itemDocumentsPath, itemDocumentDto.id_item.ToString()), itemDocumentDto.document);
        var itemDocument = new ItemsDocuments
        {
            id_item = itemDocumentDto.id_item,
            url_item_document = savedFile.url,
            name_item_document = itemDocumentDto.name_item_document,
            type_item_document = savedFile.mimeType,
            size_item_document = itemDocumentDto.document.Length
        };
        await _context.ItemsDocuments.AddAsync(itemDocument);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemDocumentDto>(itemDocument);
    }

    public async Task<ReadItemDocumentDto> UpdateItemDocument(int id, UpdateItemDocumentDto itemDocumentDto, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ItemDocument with id '{id}' not found");
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found for item with id '{itemId}'");
        }
        if (itemDocumentDto.name_item_document is not null)
        {
            itemDocument.name_item_document = itemDocumentDto.name_item_document;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadItemDocumentDto>(itemDocument);
    }

    public async Task DeleteItemDocument(int id, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ItemDocument with id '{id}' not found");
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemDocument with id '{id}' not found for item with id '{itemId}'");
        }
        await _fileService.DeleteFile(itemDocument.url_item_document);
        _context.ItemsDocuments.Remove(itemDocument);
        await _context.SaveChangesAsync();
    }
}