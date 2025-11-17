using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
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

    public async Task<IEnumerable<ReadItemDocumentDto>> GetItemsDocumentsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ItemsDocuments.AsQueryable();
        query = query.Where(id => id.id_item == itemId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(id => id.id_item_document);
        var itemDocument = await query.ToListAsync();
        return _mapper.Map<List<ReadItemDocumentDto>>(itemDocument);
    }

    public async Task<int> GetItemsDocumentsCountByItemId(int itemId)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        return await _context.ItemsDocuments
            .Where(id => id.id_item == itemId)
            .CountAsync();
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