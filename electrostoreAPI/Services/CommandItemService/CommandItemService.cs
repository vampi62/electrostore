using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandItemService;

public class CommandItemService : ICommandItemService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CommandItemService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedCommandItemDto>> GetCommandsItemsByCommandId(int commandId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id '{commandId}' not found");
        }
        var query = _context.CommandsItems.AsQueryable();
        query = query.Where(ci => ci.id_command == commandId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(ci => ci.id_item);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ci => ci.Item);
        }
        if (expand != null && expand.Contains("command"))
        {
            query = query.Include(ci => ci.Command);
        }
        var commandItem = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedCommandItemDto>>(commandItem);
    }

    public async Task<int> GetCommandsItemsCountByCommandId(int commandId)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id '{commandId}' not found");
        }
        return await _context.CommandsItems
            .CountAsync(ci => ci.id_command == commandId);
    }

    public async Task<IEnumerable<ReadExtendedCommandItemDto>> GetCommandsItemsByItemId(int itemId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.CommandsItems.AsQueryable();
        query = query.Where(ci => ci.id_item == itemId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(ci => ci.id_command);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ci => ci.Item);
        }
        if (expand != null && expand.Contains("command"))
        {
            query = query.Include(ci => ci.Command);
        }
        var commandItem = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedCommandItemDto>>(commandItem);
    }

    public async Task<int> GetCommandsItemsCountByItemId(int itemId)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        return await _context.CommandsItems
            .CountAsync(ci => ci.id_item == itemId);
    }

    public async Task<ReadExtendedCommandItemDto> GetCommandItemById(int commandId, int itemId, List<string>? expand = null)
    {
        var query = _context.CommandsItems.AsQueryable();
        query = query.Where(ci => ci.id_command == commandId && ci.id_item == itemId);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(ci => ci.Item);
        }
        if (expand != null && expand.Contains("command"))
        {
            query = query.Include(ci => ci.Command);
        }
        var commandItem = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"CommandItem with commandId '{commandId}' and itemId '{itemId}' not found");
        return _mapper.Map<ReadExtendedCommandItemDto>(commandItem);
    }

    public async Task<ReadCommandItemDto> CreateCommandItem(CreateCommandItemDto commandItemDto)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == commandItemDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{commandItemDto.id_item}' not found");
        }
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandItemDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id '{commandItemDto.id_command}' not found");
        }
        // check if the command item already exists
        if (await _context.CommandsItems.AnyAsync(ci => ci.id_command == commandItemDto.id_command && ci.id_item == commandItemDto.id_item))
        {
            throw new ArgumentException($"CommandItem with commandId '{commandItemDto.id_command}' and itemId '{commandItemDto.id_item}' already exists");
        }
        var newCommandItem = _mapper.Map<CommandsItems>(commandItemDto);
        _context.CommandsItems.Add(newCommandItem);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandItemDto>(newCommandItem);
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
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id '{commandId}' not found");
        }
        var commandItemToUpdate = await _context.CommandsItems.FindAsync(commandId, itemId) ?? throw new KeyNotFoundException($"CommandItem with commandId '{commandId}' and itemId '{itemId}' not found");
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
        return _mapper.Map<ReadCommandItemDto>(commandItemToUpdate);
    }

    public async Task DeleteCommandItem(int commandId, int itemId)
    {
        var commandItemToDelete = await _context.CommandsItems.FindAsync(commandId, itemId) ?? throw new KeyNotFoundException($"CommandItem with commandId '{commandId}' and itemId '{itemId}' not found");
        _context.CommandsItems.Remove(commandItemToDelete);
        await _context.SaveChangesAsync();
    }
}