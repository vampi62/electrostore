using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemDocumentService;

public class ItemDocumentService : IItemDocumentService
{
    private readonly ApplicationDbContext _context;

    public ItemDocumentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadItemDocumentDto>> GetItemsDocumentsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        return await _context.ItemsDocuments
            .Where(id => id.id_item == itemId)
            .Skip(offset)
            .Take(limit)
            .Select(itemDocument => new ReadItemDocumentDto
            {
                id_item_document = itemDocument.id_item_document,
                id_item = itemDocument.id_item,
                url_item_document = itemDocument.url_item_document,
                name_item_document = itemDocument.name_item_document,
                type_item_document = itemDocument.type_item_document,
                size_item_document = itemDocument.size_item_document,
                date_item_document = itemDocument.date_item_document
            })
            .ToListAsync();
    }

    public async Task<int> GetItemsDocumentsCountByItemId(int itemId)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        return await _context.ItemsDocuments
            .Where(id => id.id_item == itemId)
            .CountAsync();
    }

    public async Task<ReadItemDocumentDto> GetItemDocumentById(int id, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ItemDocument with id {id} not found");
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemDocument with id {id} not found for item with id {itemId}");
        }
        return new ReadItemDocumentDto
        {
                id_item_document = itemDocument.id_item_document,
                id_item = itemDocument.id_item,
                url_item_document = itemDocument.url_item_document,
                name_item_document = itemDocument.name_item_document,
                type_item_document = itemDocument.type_item_document,
                size_item_document = itemDocument.size_item_document,
                date_item_document = itemDocument.date_item_document
        };
    }

    public async Task<ReadItemDocumentDto> CreateItemDocument(CreateItemDocumentDto itemDocumentDto)
    {
        // check if item exists
        if (!await _context.Items.AnyAsync(item => item.id_item == itemDocumentDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id {itemDocumentDto.id_item} not found");
        }
        var fileName = Path.GetFileNameWithoutExtension(itemDocumentDto.document.FileName);
        var fileExt = Path.GetExtension(itemDocumentDto.document.FileName);
        var i = 1;
        // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/itemDocuments"
        // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
        var newName = itemDocumentDto.document.FileName;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemDocumentDto.id_item.ToString(), newName)))
        {
            newName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemDocumentDto.id_item.ToString(), newName);
        using (var fileStream = new FileStream(savePath, FileMode.Create))
        {
            await itemDocumentDto.document.CopyToAsync(fileStream);
        }
        var itemDocument = new ItemsDocuments
        {
            id_item = itemDocumentDto.id_item,
            url_item_document = itemDocumentDto.id_item + "/" + newName,
            name_item_document = itemDocumentDto.name_item_document,
            type_item_document = itemDocumentDto.type_item_document,
            size_item_document = itemDocumentDto.document.Length,
            date_item_document = DateTime.Now
        };
        await _context.ItemsDocuments.AddAsync(itemDocument);
        await _context.SaveChangesAsync();
        return new ReadItemDocumentDto
        {
            id_item_document = itemDocument.id_item_document,
            id_item = itemDocument.id_item,
            url_item_document = itemDocument.url_item_document,
            name_item_document = itemDocument.name_item_document,
            type_item_document = itemDocument.type_item_document,
            size_item_document = itemDocument.size_item_document,
            date_item_document = itemDocument.date_item_document
        };
    }

    public async Task<ReadItemDocumentDto> UpdateItemDocument(int id, UpdateItemDocumentDto itemDocumentDto, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ItemDocument with id {id} not found");
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemDocument with id {id} not found for item with id {itemId}");
        }
        if (itemDocumentDto.document is not null)
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemDocument.id_item.ToString(), itemDocument.url_item_document);
            if (itemDocumentDto.document.Length == 0)
            {
                throw new ArgumentException("Document file is required");
            }
            if (itemDocumentDto.document.Length > (30 * 1024 * 1024)) // 30MB max
            {
                throw new ArgumentException("Document file size should not exceed 30MB");
            }
            var fileName = Path.GetFileNameWithoutExtension(itemDocumentDto.document.FileName);
            var fileExt = Path.GetExtension(itemDocumentDto.document.FileName);
            if (!new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" }.Contains(fileExt)) // if extension is not allowed
            {
                throw new ArgumentException("Document file extension not allowed");
            }
            var i = 1;
            // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/itemDocuments"
            // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
            var newName = itemDocumentDto.document.FileName;
            while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemDocument.id_item.ToString(), newName)))
            {
                newName = $"{fileName}({i}){fileExt}";
                i++;
            }
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemDocument.id_item.ToString(), newName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await itemDocumentDto.document.CopyToAsync(fileStream);
            }
            itemDocument.url_item_document = itemDocument.id_item + "/" + newName;
            itemDocument.size_item_document = itemDocumentDto.document.Length;
            itemDocument.date_item_document = DateTime.Now;
            // remove old file
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }
        if (itemDocumentDto.name_item_document is not null)
        {
            itemDocument.name_item_document = itemDocumentDto.name_item_document;
        }
        if (itemDocumentDto.type_item_document is not null)
        {
            itemDocument.type_item_document = itemDocumentDto.type_item_document;
        }
        await _context.SaveChangesAsync();
        return new ReadItemDocumentDto
        {
            id_item_document = itemDocument.id_item_document,
            id_item = itemDocument.id_item,
            url_item_document = itemDocument.url_item_document,
            name_item_document = itemDocument.name_item_document,
            type_item_document = itemDocument.type_item_document,
            size_item_document = itemDocument.size_item_document,
            date_item_document = itemDocument.date_item_document
        };
    }

    public async Task DeleteItemDocument(int id, int? itemId = null)
    {
        var itemDocument = await _context.ItemsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ItemDocument with id {id} not found");
        if (itemId is not null && itemDocument.id_item != itemId)
        {
            throw new KeyNotFoundException($"ItemDocument with id {id} not found for item with id {itemId}");
        }
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemDocument.id_item.ToString(), itemDocument.url_item_document);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        _context.ItemsDocuments.Remove(itemDocument);
        await _context.SaveChangesAsync();
    }

    public async Task<GetFileResult> GetFile(string url)
    {
        var pathImg = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", url);
        if (!File.Exists(pathImg))
        {
            return new GetFileResult
            {
                Success = false,
                ErrorMessage = "File not found",
                FilePath = "",
                MimeType = ""
            };
        } else {
            var ext = Path.GetExtension(pathImg);
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
            return await Task.FromResult(new GetFileResult
            {
                Success = true,
                FilePath = pathImg,
                MimeType = mimeType
            });
        }
    }
}