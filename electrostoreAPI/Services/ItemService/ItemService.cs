using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ItemService;

public class ItemService : IItemService
{
    private readonly ApplicationDbContext _context;

    public ItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadItemDto>> GetItems(int limit = 100, int offset = 0)
    {
        return await _context.Items
            .Skip(offset)
            .Take(limit)
            .Select(item => new ReadItemDto
            {
                id_item = item.id_item,
                id_img = item.id_img,
                nom_item = item.nom_item,
                seuil_min_item = item.seuil_min_item,
                datasheet_item = item.datasheet_item,
                description_item = item.description_item,
            })
            .ToListAsync();
    }

    public async Task<ActionResult<ReadItemDto>> GetItemById(int itemId)
    {
        var item = await _context.Items.FindAsync(itemId);
        if (item == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } } });
        }

        return new ReadItemDto
        {
            id_item = item.id_item,
            id_img = item.id_img,
            nom_item = item.nom_item,
            seuil_min_item = item.seuil_min_item,
            datasheet_item = item.datasheet_item,
            description_item = item.description_item,
        };
    }

    public async Task<ActionResult<ReadItemDto>> CreateItem(CreateItemDto itemDto)
    {
        // check if img exists
        if (itemDto.id_img != null && !await _context.Imgs.AnyAsync(i => i.id_img == itemDto.id_img))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_img = new string[] { "Img not found" } } });
        }

        // check if item already exists
        if (await _context.Items.AnyAsync(i => i.nom_item == itemDto.nom_item))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { nom_item = new string[] { "Item name already exists" } } });
        }

        var item = new Items
        {
            id_img = itemDto.id_img,
            nom_item = itemDto.nom_item,
            seuil_min_item = itemDto.seuil_min_item,
            datasheet_item = itemDto.datasheet_item,
            description_item = itemDto.description_item,
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return new ReadItemDto
        {
            id_item = item.id_item,
            id_img = item.id_img,
            nom_item = item.nom_item,
            seuil_min_item = item.seuil_min_item,
            datasheet_item = item.datasheet_item,
            description_item = item.description_item,
        };
    }

    public async Task<ActionResult<ReadItemDto>> UpdateItem(int id, UpdateItemDto itemDto)
    {
        // check if img exists

        var itemToUpdate = await _context.Items.FindAsync(id);
        if (itemToUpdate == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } } });
        }

        if (itemDto.nom_item != null)
        {
            // check if item already exists
            if (await _context.Items.AnyAsync(i => i.nom_item == itemDto.nom_item))
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { nom_item = new string[] { "Item name already exists" } } });
            }
            itemToUpdate.nom_item = itemDto.nom_item;
        }

        if (itemDto.seuil_min_item != null)
        {
            itemToUpdate.seuil_min_item = itemDto.seuil_min_item.Value;
        }

        if (itemDto.datasheet_item != null)
        {
            itemToUpdate.datasheet_item = itemDto.datasheet_item;
        }

        if (itemDto.description_item != null)
        {
            itemToUpdate.description_item = itemDto.description_item;
        }

        if (itemDto.id_img != null)
        {
            var img = await _context.Imgs.FindAsync(itemDto.id_img);
            if ((img == null) || (id != img.id_item))
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_img = new string[] { "Img not found" } } });
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
            datasheet_item = itemToUpdate.datasheet_item,
            description_item = itemToUpdate.description_item,
        };
    }

    public async Task<IActionResult> DeleteItem(int itemId)
    {
        var itemToDelete = await _context.Items.FindAsync(itemId);
        if (itemToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } } });
        }
        _context.Items.Remove(itemToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}