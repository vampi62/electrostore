using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandItemService;

public class CommandItemService : ICommandItemService
{
    private readonly ApplicationDbContext _context;

    public CommandItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadCommandItemDto>> GetCommandItemsByCommandId(int commandId, int limit = 100, int offset = 0)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new ArgumentException("Command not found");
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
                prix_commanditem = ci.prix_commanditem
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadCommandItemDto>> GetCommandItemsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new ArgumentException("Item not found");
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
                prix_commanditem = ci.prix_commanditem
            })
            .ToListAsync();
    }

    public async Task<ReadCommandItemDto> GetCommandItemById(int commandId, int itemId)
    {
        var commandItem = await _context.CommandsItems.FindAsync(commandId, itemId);
        if (commandItem == null)
        {
            throw new ArgumentException("Command item not found");
        }

        return new ReadCommandItemDto
        {
            id_item = commandItem.id_item,
            id_command = commandItem.id_command,
            qte_commanditem = commandItem.qte_commanditem,
            prix_commanditem = commandItem.prix_commanditem
        };
    }

    public async Task<ReadCommandItemDto> CreateCommandItem(CreateCommandItemDto commandItemDto)
    {
        if (commandItemDto.qte_commanditem <= 0)
        {
            throw new ArgumentException("Quantity must be greater than 0");
        }
        if (commandItemDto.prix_commanditem <= 0)
        {
            throw new ArgumentException("Price must be greater than 0");
        }

        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == commandItemDto.id_item))
        {
            throw new ArgumentException("Item not found");
        }

        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandItemDto.id_command))
        {
            throw new ArgumentException("Command not found");
        }

        // check if the command item already exists
        if (await _context.CommandsItems.AnyAsync(ci => ci.id_command == commandItemDto.id_command && ci.id_item == commandItemDto.id_item))
        {
            throw new ArgumentException("Command item already exists");
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
            prix_commanditem = newCommandItem.prix_commanditem
        };
    }

    public async Task<ReadCommandItemDto> UpdateCommandItem(int commandId, int itemId, UpdateCommandItemDto commandItemDto)
    {
        if (commandItemDto.qte_commanditem <= 0 && commandItemDto.prix_commanditem <= 0)
        {
            throw new ArgumentException("Quantity and price must be greater than 0");
        }

        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new ArgumentException("Item not found");
        }

        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new ArgumentException("Command not found");
        }

        var commandItemToUpdate = await _context.CommandsItems.FindAsync(commandId, itemId);
        if (commandItemToUpdate == null)
        {
            throw new ArgumentException("Command item not found");
        }

        if (commandItemDto.qte_commanditem != null)
        {
            if (commandItemDto.qte_commanditem <= 0)
            {
                throw new ArgumentException("Quantity must be greater than 0");
            }
            commandItemToUpdate.qte_commanditem = commandItemDto.qte_commanditem.Value;
        }

        if (commandItemDto.prix_commanditem != null)
        {
            if (commandItemDto.prix_commanditem <= 0)
            {
                throw new ArgumentException("Price must be greater than 0");
            }
            commandItemToUpdate.prix_commanditem = commandItemDto.prix_commanditem.Value;
        }

        await _context.SaveChangesAsync();

        return new ReadCommandItemDto
        {
            id_item = commandItemToUpdate.id_item,
            id_command = commandItemToUpdate.id_command,
            qte_commanditem = commandItemToUpdate.qte_commanditem,
            prix_commanditem = commandItemToUpdate.prix_commanditem
        };
    }

    public async Task DeleteCommandItem(int commandId, int itemId)
    {
        var commandItemToDelete = await _context.CommandsItems.FindAsync(commandId, itemId);
        if (commandItemToDelete == null)
        {
            throw new ArgumentException("Command item not found");
        }

        _context.CommandsItems.Remove(commandItemToDelete);
        await _context.SaveChangesAsync();
    }
}