using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandService;

public class CommandService : ICommandService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CommandService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedCommandDto>> GetCommands(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Commands.AsQueryable();
        if (idResearch != null)
        {
            query = query.Where(b => idResearch.Contains(b.id_command));
        }
        query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("commands_commentaires"))
        {
            query = query.Include(c => c.CommandsCommentaires);
        }
        if (expand != null && expand.Contains("commands_documents"))
        {
            query = query.Include(c => c.CommandsDocuments);
        }
        if (expand != null && expand.Contains("commands_items"))
        {
            query = query.Include(c => c.CommandsItems);
        }
        var command = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedCommandDto>>(command);
    }

    public async Task<int> GetCommandsCount()
    {
        return await _context.Commands.CountAsync();
    }

    public async Task<ReadExtendedCommandDto> GetCommandById(int id, List<string>? expand = null)
    {
        var query = _context.Commands.AsQueryable();
        query = query.Where(c => c.id_command == id);
        if (expand != null && expand.Contains("commands_commentaires"))
        {
            query = query.Include(c => c.CommandsCommentaires);
        }
        if (expand != null && expand.Contains("commands_documents"))
        {
            query = query.Include(c => c.CommandsDocuments);
        }
        if (expand != null && expand.Contains("commands_items"))
        {
            query = query.Include(c => c.CommandsItems);
        }
        var command = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Command with id {id} not found");
        return _mapper.Map<ReadExtendedCommandDto>(command);
    }

    public async Task<ReadCommandDto> CreateCommand(CreateCommandDto commandDto)
    {
        var newCommand = new Commands
        {
            prix_command = commandDto.prix_command,
            url_command = commandDto.url_command,
            status_command = commandDto.status_command,
            date_command = commandDto.date_command,
            date_livraison_command = commandDto.date_livraison_command
        };
        _context.Commands.Add(newCommand);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", newCommand.id_command.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", newCommand.id_command.ToString()));
        }
        return _mapper.Map<ReadCommandDto>(newCommand);
    }

    public async Task<ReadCommandDto> UpdateCommand(int id, UpdateCommandDto commandDto)
    {
        var commandToUpdate = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id {id} not found");
        if (commandDto.prix_command is not null)
        {
            commandToUpdate.prix_command = commandDto.prix_command.Value;
        }
        if (commandDto.url_command is not null)
        {
            commandToUpdate.url_command = commandDto.url_command;
        }
        if (commandDto.status_command is not null)
        {
            commandToUpdate.status_command = commandDto.status_command;
        }
        if (commandDto.date_command is not null)
        {
            commandToUpdate.date_command = commandDto.date_command.Value;
        }
        if (commandDto.date_livraison_command is not null)
        {
            commandToUpdate.date_livraison_command = commandDto.date_livraison_command;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDto>(commandToUpdate);
    }

    public async Task DeleteCommand(int id)
    {
        var commandToDelete = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id {id} not found");
        _context.Commands.Remove(commandToDelete);
        await _context.SaveChangesAsync();
        //remove folder in wwwroot/commandDocuments
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", id.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", id.ToString()), true);
        }
    }
}
