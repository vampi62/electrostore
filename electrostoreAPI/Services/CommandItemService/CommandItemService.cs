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

    public async Task<IEnumerable<ReadExtendedCommandItemDto>> GetCommandItemsByCommandId(int commandId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        return await _context.CommandsItems
            .Skip(offset)
            .Take(limit)
            .Where(ci => ci.id_command == commandId)
            .Select(ci => new ReadExtendedCommandItemDto
            {
                id_item = ci.id_item,
                id_command = ci.id_command,
                qte_command_item = ci.qte_command_item,
                prix_command_item = ci.prix_command_item,
                item = expand != null && expand.Contains("item") ? new ReadItemDto
                {
                    id_item = ci.Item.id_item,
                    nom_item = ci.Item.nom_item,
                    seuil_min_item = ci.Item.seuil_min_item,
                    description_item = ci.Item.description_item,
                    id_img = ci.Item.id_img
                } : null,
                command = expand != null && expand.Contains("command") ? new ReadCommandDto
                {
                    id_command = ci.Command.id_command,
                    prix_command = ci.Command.prix_command,
                    url_command = ci.Command.url_command,
                    status_command = ci.Command.status_command,
                    date_command = ci.Command.date_command,
                    date_livraison_command = ci.Command.date_livraison_command
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetCommandItemsCountByCommandId(int commandId)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        return await _context.CommandsItems
            .CountAsync(ci => ci.id_command == commandId);
    }

    public async Task<IEnumerable<ReadExtendedCommandItemDto>> GetCommandItemsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        return await _context.CommandsItems
            .Skip(offset)
            .Take(limit)
            .Where(ci => ci.id_item == itemId)
            .Select(ci => new ReadExtendedCommandItemDto
            {
                id_item = ci.id_item,
                id_command = ci.id_command,
                qte_command_item = ci.qte_command_item,
                prix_command_item = ci.prix_command_item,
                item = expand != null && expand.Contains("item") ? new ReadItemDto
                {
                    id_item = ci.Item.id_item,
                    nom_item = ci.Item.nom_item,
                    seuil_min_item = ci.Item.seuil_min_item,
                    description_item = ci.Item.description_item,
                    id_img = ci.Item.id_img
                } : null,
                command = expand != null && expand.Contains("command") ? new ReadCommandDto
                {
                    id_command = ci.Command.id_command,
                    prix_command = ci.Command.prix_command,
                    url_command = ci.Command.url_command,
                    status_command = ci.Command.status_command,
                    date_command = ci.Command.date_command,
                    date_livraison_command = ci.Command.date_livraison_command
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetCommandItemsCountByItemId(int itemId)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        return await _context.CommandsItems
            .CountAsync(ci => ci.id_item == itemId);
    }

    public async Task<ReadExtendedCommandItemDto> GetCommandItemById(int commandId, int itemId, List<string>? expand = null)
    {
        var commandItem = await _context.CommandsItems.FindAsync(commandId, itemId) ?? throw new KeyNotFoundException($"CommandItem with commandId {commandId} and itemId {itemId} not found");
        return new ReadExtendedCommandItemDto
        {
            id_item = commandItem.id_item,
            id_command = commandItem.id_command,
            qte_command_item = commandItem.qte_command_item,
            prix_command_item = commandItem.prix_command_item,
            item = expand != null && expand.Contains("item") ? new ReadItemDto
            {
                id_item = commandItem.Item.id_item,
                nom_item = commandItem.Item.nom_item,
                seuil_min_item = commandItem.Item.seuil_min_item,
                description_item = commandItem.Item.description_item,
                id_img = commandItem.Item.id_img
            } : null,
            command = expand != null && expand.Contains("command") ? new ReadCommandDto
            {
                id_command = commandItem.Command.id_command,
                prix_command = commandItem.Command.prix_command,
                url_command = commandItem.Command.url_command,
                status_command = commandItem.Command.status_command,
                date_command = commandItem.Command.date_command,
                date_livraison_command = commandItem.Command.date_livraison_command
            } : null
        };
    }

    public async Task<ReadCommandItemDto> CreateCommandItem(CreateCommandItemDto commandItemDto)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == commandItemDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id {commandItemDto.id_item} not found");
        }
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandItemDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id {commandItemDto.id_command} not found");
        }
        // check if the command item already exists
        if (await _context.CommandsItems.AnyAsync(ci => ci.id_command == commandItemDto.id_command && ci.id_item == commandItemDto.id_item))
        {
            throw new ArgumentException($"CommandItem with commandId {commandItemDto.id_command} and itemId {commandItemDto.id_item} already exists");
        }
        var newCommandItem = new CommandsItems
        {
            id_item = commandItemDto.id_item,
            id_command = commandItemDto.id_command,
            qte_command_item = commandItemDto.qte_command_item,
            prix_command_item = commandItemDto.prix_command_item
        };
        _context.CommandsItems.Add(newCommandItem);
        await _context.SaveChangesAsync();
        return new ReadCommandItemDto
        {
            id_item = newCommandItem.id_item,
            id_command = newCommandItem.id_command,
            qte_command_item = newCommandItem.qte_command_item,
            prix_command_item = newCommandItem.prix_command_item
        };
    }

    public async Task<ReadBulkCommandItemDto> CreateBulkCommandItem(List<CreateCommandItemDto> commandItemBulkDto)
    {
        var validQuery = new List<ReadCommandItemDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var commandItemDto in commandItemBulkDto)
        {
            try
            {
                validQuery.Add(await CreateCommandItem(commandItemDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = commandItemDto
                });
            }
        }
        return new ReadBulkCommandItemDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadCommandItemDto> UpdateCommandItem(int commandId, int itemId, UpdateCommandItemDto commandItemDto)
    {
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        var commandItemToUpdate = await _context.CommandsItems.FindAsync(commandId, itemId) ?? throw new KeyNotFoundException($"CommandItem with commandId {commandId} and itemId {itemId} not found");
        if (commandItemDto.qte_command_item is not null)
        {
            if (commandItemDto.qte_command_item <= 0)
            {
                throw new ArgumentException("qte_command_item must be greater than 0");
            }
            commandItemToUpdate.qte_command_item = commandItemDto.qte_command_item.Value;
        }
        if (commandItemDto.prix_command_item is not null)
        {
            if (commandItemDto.prix_command_item <= 0)
            {
                throw new ArgumentException("prix_command_item must be greater than 0");
            }
            commandItemToUpdate.prix_command_item = commandItemDto.prix_command_item.Value;
        }
        await _context.SaveChangesAsync();
        return new ReadCommandItemDto
        {
            id_item = commandItemToUpdate.id_item,
            id_command = commandItemToUpdate.id_command,
            qte_command_item = commandItemToUpdate.qte_command_item,
            prix_command_item = commandItemToUpdate.prix_command_item
        };
    }

    public async Task DeleteCommandItem(int commandId, int itemId)
    {
        var commandItemToDelete = await _context.CommandsItems.FindAsync(commandId, itemId) ?? throw new KeyNotFoundException($"CommandItem with commandId {commandId} and itemId {itemId} not found");
        _context.CommandsItems.Remove(commandItemToDelete);
        await _context.SaveChangesAsync();
    }
}