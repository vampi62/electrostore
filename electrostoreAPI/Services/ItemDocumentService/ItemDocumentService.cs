using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.ItemDocumentService;

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
        rsql.Add(new FilterDto { field = "id_item", search_type = "eq", value = itemId.ToString() });
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_item_document", order = "asc" })
            .ToResponseAsync(itemDocument => _mapper.Map<List<ReadItemDocumentDto>>(itemDocument));
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
        var savedFile = await _fileService.SaveFile(Path.Combine(_itemDocumentsPath, itemDocumentDto.id_item.ToString()), itemDocumentDto.document.FileName, itemDocumentDto.document.ContentType, itemDocumentDto.document.OpenReadStream());
        var itemDocument = new ItemsDocuments
        {
            id_item = itemDocumentDto.id_item,
            url_item_document = savedFile.path,
            name_item_document = itemDocumentDto.name_item_document,
            type_item_document = savedFile.mime_type,
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