using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace electrostore.Services.ItemService;

public class ItemService : IItemService
{
    private readonly ApplicationDbContext _context;

    public ItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedItemDto>> GetItems(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Items.AsQueryable();
        if (idResearch != null && idResearch.Any())
        {
            query = query.Where(b => idResearch.Contains(b.id_item));
        }
        return await query
            .Skip(offset)
            .Take(limit)
            .Select(item => new ReadExtendedItemDto
            {
                id_item = item.id_item,
                id_img = item.id_img,
                nom_item = item.nom_item,
                seuil_min_item = item.seuil_min_item,
                description_item = item.description_item,
                item_tags = expand != null && expand.Contains("item_tags") ? item.ItemsTags.Select(it => new ReadItemTagDto
                {
                    id_item = it.id_item,
                    id_tag = it.id_tag
                }) : null,
                item_boxs = expand != null && expand.Contains("item_boxs") ? item.ItemsBoxs.Select(ib => new ReadItemBoxDto
                {
                    id_item = ib.id_item,
                    id_box = ib.id_box,
                    qte_item_box = ib.qte_item_box,
                    seuil_max_item_item_box = ib.seuil_max_item_item_box
                }) : null,
                command_items = expand != null && expand.Contains("command_items") ? item.CommandsItems.Select(ci => new ReadCommandItemDto
                {
                    id_command = ci.id_command,
                    id_item = ci.id_item,
                    qte_command_item = ci.qte_command_item,
                    prix_command_item = ci.prix_command_item
                }) : null,
                projet_items = expand != null && expand.Contains("projet_items") ? item.ProjetsItems.Select(pi => new ReadProjetItemDto
                {
                    id_projet = pi.id_projet,
                    id_item = pi.id_item,
                    qte_projet_item = pi.qte_projet_item
                }) : null,
                item_documents = expand != null && expand.Contains("item_documents") ? item.ItemsDocuments.Select(id => new ReadItemDocumentDto
                {
                    id_item_document = id.id_item_document,
                    url_item_document = id.url_item_document,
                    name_item_document = id.name_item_document,
                    type_item_document = id.type_item_document,
                    size_item_document = id.size_item_document,
                    date_item_document = id.date_item_document,
                    id_item = id.id_item
                }) : null,
                item_tags_count = item.ItemsTags.Count,
                item_boxs_count = item.ItemsBoxs.Count,
                command_items_count = item.CommandsItems.Count,
                projet_items_count = item.ProjetsItems.Count,
                item_documents_count = item.ItemsDocuments.Count
            })
            .ToListAsync();
    }

    public async Task<int> GetItemsCount()
    {
        return await _context.Items.CountAsync();
    }

    public async Task<ReadExtendedItemDto> GetItemById(int itemId, List<string>? expand = null)
    {
        var item = await _context.Items.FindAsync(itemId) ?? throw new KeyNotFoundException($"Item with id {itemId} not found");
        return new ReadExtendedItemDto
        {
            id_item = item.id_item,
            id_img = item.id_img,
            nom_item = item.nom_item,
            seuil_min_item = item.seuil_min_item,
            description_item = item.description_item,
            item_tags_count = item.ItemsTags.Count,
            item_boxs_count = item.ItemsBoxs.Count,
            command_items_count = item.CommandsItems.Count,
            projet_items_count = item.ProjetsItems.Count,
            item_documents_count = item.ItemsDocuments.Count,
            item_tags = expand != null && expand.Contains("item_tags") ? item.ItemsTags.Select(it => new ReadItemTagDto
            {
                id_item = it.id_item,
                id_tag = it.id_tag
            }) : null,
            item_boxs = expand != null && expand.Contains("item_boxs") ? item.ItemsBoxs.Select(ib => new ReadItemBoxDto
            {
                id_item = ib.id_item,
                id_box = ib.id_box,
                qte_item_box = ib.qte_item_box,
                seuil_max_item_item_box = ib.seuil_max_item_item_box
            }) : null,
            command_items = expand != null && expand.Contains("command_items") ? item.CommandsItems.Select(ci => new ReadCommandItemDto
            {
                id_command = ci.id_command,
                id_item = ci.id_item,
                qte_command_item = ci.qte_command_item,
                prix_command_item = ci.prix_command_item
            }) : null,
            projet_items = expand != null && expand.Contains("projet_items") ? item.ProjetsItems.Select(pi => new ReadProjetItemDto
            {
                id_projet = pi.id_projet,
                id_item = pi.id_item,
                qte_projet_item = pi.qte_projet_item
            }) : null,
            item_documents = expand != null && expand.Contains("item_documents") ? item.ItemsDocuments.Select(id => new ReadItemDocumentDto
            {
                id_item_document = id.id_item_document,
                url_item_document = id.url_item_document,
                name_item_document = id.name_item_document,
                type_item_document = id.type_item_document,
                size_item_document = id.size_item_document,
                date_item_document = id.date_item_document,
                id_item = id.id_item
            }) : null
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
        return new ReadItemDto
        {
            id_item = item.id_item,
            id_img = item.id_img,
            nom_item = item.nom_item,
            seuil_min_item = item.seuil_min_item,
            description_item = item.description_item,
        };
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
        return new ReadItemDto
        {
            id_item = itemToUpdate.id_item,
            id_img = itemToUpdate.id_img,
            nom_item = itemToUpdate.nom_item,
            seuil_min_item = itemToUpdate.seuil_min_item,
            description_item = itemToUpdate.description_item,
        };
    }

    public async Task DeleteItem(int itemId)
    {
        var itemToDelete = await _context.Items.FindAsync(itemId) ?? throw new KeyNotFoundException($"Item with id {itemId} not found");
        _context.Items.Remove(itemToDelete);
        await _context.SaveChangesAsync();
        //remove folder in wwwroot/images
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", itemId.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", itemId.ToString()), true);
        }
        //remove folder in wwwroot/itemDocuments
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemId.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/itemDocuments", itemId.ToString()), true);
        }
    }
}