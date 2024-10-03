using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandItemService;

public class CommandItemService : ICommandItemService
{
    private readonly ApplicationDbContext _context;

    public CommandItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ActionResult<IEnumerable<ReadCommandItemDto>>> GetCommandItemsByCommandId(int commandId, int limit = 100, int offset = 0)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_command = new string[] { "Command not found" } } });
        }
        return await _context.CommandsItems
            .Skip(offset)
            .Take(limit)
            .Where(ci => ci.id_command == commandId)
            .Select(ci => new ReadCommandItemDto
            {
                id_item = ci.id_item,
                id_command = ci.id_command,
                qte_commanditem = ci.qte_commanditem,
                prix_commanditem = ci.prix_commanditem,
                item = new ReadItemDto
                {
                    id_item = ci.Item.id_item,
                    nom_item = ci.Item.nom_item,
                    seuil_min_item = ci.Item.seuil_min_item,
                    datasheet_item = ci.Item.datasheet_item,
                    description_item = ci.Item.description_item,
                    id_img = ci.Item.id_img
                }
            })
            .ToListAsync();
    }

    public async Task<ActionResult<IEnumerable<ReadCommandItemDto>>> GetCommandItemsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } }});
        }

        return await _context.CommandsItems
            .Skip(offset)
            .Take(limit)
            .Where(ci => ci.id_item == itemId)
            .Select(ci => new ReadCommandItemDto
            {
                id_item = ci.id_item,
                id_command = ci.id_command,
                qte_commanditem = ci.qte_commanditem,
                prix_commanditem = ci.prix_commanditem,
                item = new ReadItemDto
                {
                    id_item = ci.Item.id_item,
                    nom_item = ci.Item.nom_item,
                    seuil_min_item = ci.Item.seuil_min_item,
                    datasheet_item = ci.Item.datasheet_item,
                    description_item = ci.Item.description_item,
                    id_img = ci.Item.id_img
                }
            })
            .ToListAsync();
    }

    public async Task<ActionResult<ReadCommandItemDto>> GetCommandItemById(int commandId, int itemId)
    {
        var commandItem = await _context.CommandsItems.FindAsync(commandId, itemId);
        if (commandItem == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_command = new string[] { "Command item not found" } } });
        }

        return new ReadCommandItemDto
        {
            id_item = commandItem.id_item,
            id_command = commandItem.id_command,
            qte_commanditem = commandItem.qte_commanditem,
            prix_commanditem = commandItem.prix_commanditem,
            item = new ReadItemDto
            {
                id_item = commandItem.Item.id_item,
                nom_item = commandItem.Item.nom_item,
                seuil_min_item = commandItem.Item.seuil_min_item,
                datasheet_item = commandItem.Item.datasheet_item,
                description_item = commandItem.Item.description_item,
                id_img = commandItem.Item.id_img
            }
        };
    }

    public async Task<ActionResult<ReadCommandItemDto>> CreateCommandItem(CreateCommandItemDto commandItemDto)
    {
        if (commandItemDto.qte_commanditem <= 0)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { qte_commanditem = new string[] { "Quantity must be greater than 0" } }});
        }
        if (commandItemDto.prix_commanditem <= 0)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { prix_commanditem = new string[] { "Price must be greater than 0" } }});
        }

        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == commandItemDto.id_item))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } }});
        }

        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandItemDto.id_command))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_command = new string[] { "Command not found" } } });
        }

        // check if the command item already exists
        if (await _context.CommandsItems.AnyAsync(ci => ci.id_command == commandItemDto.id_command && ci.id_item == commandItemDto.id_item))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_command = new string[] { "Command item already exists" } }});
        }

        var newCommandItem = new CommandsItems
        {
            id_item = commandItemDto.id_item,
            id_command = commandItemDto.id_command,
            qte_commanditem = commandItemDto.qte_commanditem,
            prix_commanditem = commandItemDto.prix_commanditem
        };

        _context.CommandsItems.Add(newCommandItem);
        await _context.SaveChangesAsync();

        return new ReadCommandItemDto
        {
            id_item = newCommandItem.id_item,
            id_command = newCommandItem.id_command,
            qte_commanditem = newCommandItem.qte_commanditem,
            prix_commanditem = newCommandItem.prix_commanditem,
            item = new ReadItemDto
            {
                id_item = newCommandItem.Item.id_item,
                nom_item = newCommandItem.Item.nom_item,
                seuil_min_item = newCommandItem.Item.seuil_min_item,
                datasheet_item = newCommandItem.Item.datasheet_item,
                description_item = newCommandItem.Item.description_item,
                id_img = newCommandItem.Item.id_img
            }
        };
    }

    public async Task<ActionResult<ReadCommandItemDto>> UpdateCommandItem(int commandId, int itemId, UpdateCommandItemDto commandItemDto)
    {
        if (commandItemDto.qte_commanditem <= 0 && commandItemDto.prix_commanditem <= 0)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { qte_commanditem = new string[] { "Quantity must be greater than 0" }, prix_commanditem = new string[] { "Price must be greater than 0" } }});
        }
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } }});
        }
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_command = new string[] { "Command not found" } }});
        }

        var commandItemToUpdate = await _context.CommandsItems.FindAsync(commandId, itemId);
        if (commandItemToUpdate == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_command = new string[] { "Command item not found" } }});
        }

        if (commandItemDto.qte_commanditem != null)
        {
            if (commandItemDto.qte_commanditem <= 0)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { qte_commanditem = new string[] { "Quantity must be greater than 0" } }});
            }
            commandItemToUpdate.qte_commanditem = commandItemDto.qte_commanditem.Value;
        }

        if (commandItemDto.prix_commanditem != null)
        {
            if (commandItemDto.prix_commanditem <= 0)
            {
                return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { prix_commanditem = new string[] { "Price must be greater than 0" } }});
            }
            commandItemToUpdate.prix_commanditem = commandItemDto.prix_commanditem.Value;
        }

        await _context.SaveChangesAsync();

        return new ReadCommandItemDto
        {
            id_item = commandItemToUpdate.id_item,
            id_command = commandItemToUpdate.id_command,
            qte_commanditem = commandItemToUpdate.qte_commanditem,
            prix_commanditem = commandItemToUpdate.prix_commanditem,
            item = new ReadItemDto
            {
                id_item = commandItemToUpdate.Item.id_item,
                nom_item = commandItemToUpdate.Item.nom_item,
                seuil_min_item = commandItemToUpdate.Item.seuil_min_item,
                datasheet_item = commandItemToUpdate.Item.datasheet_item,
                description_item = commandItemToUpdate.Item.description_item,
                id_img = commandItemToUpdate.Item.id_img
            }
        };
    }

    public async Task<IActionResult> DeleteCommandItem(int commandId, int itemId)
    {
        var commandItemToDelete = await _context.CommandsItems.FindAsync(commandId, itemId);
        if (commandItemToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_command = new string[] { "Command item not found" } }});
        }
        _context.CommandsItems.Remove(commandItemToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}