using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandService;

public class CommandService : ICommandService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly string _commandDocumentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments");

    public CommandService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedCommandDto>> GetCommands(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Commands.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(c => idResearch.Contains(c.id_command));
        }
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(c => c.id_command);
        var command = await query
            .Select(c => new
            {
                Command = c,
                CommandsCommentairesCount = c.CommandsCommentaires.Count,
                CommandsDocumentsCount = c.CommandsDocuments.Count,
                CommandsItemsCount = c.CommandsItems.Count,
                CommandsCommentaires = expand != null && expand.Contains("commands_commentaires") ? c.CommandsCommentaires.Take(20).ToList() : null,
                CommandsDocuments = expand != null && expand.Contains("commands_documents") ? c.CommandsDocuments.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("commands_items") ? c.CommandsItems.Take(20).ToList() : null
            })
            .ToListAsync();
        return command.Select(c => {
            return _mapper.Map<ReadExtendedCommandDto>(c.Command) with
            {
                commands_commentaires_count = c.CommandsCommentairesCount,
                commands_documents_count = c.CommandsDocumentsCount,
                commands_items_count = c.CommandsItemsCount,
                commands_commentaires = _mapper.Map<IEnumerable<ReadCommandCommentaireDto>>(c.CommandsCommentaires),
                commands_documents = _mapper.Map<IEnumerable<ReadCommandDocumentDto>>(c.CommandsDocuments),
                commands_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(c.CommandsItems)
            };
        }).ToList();
    }

    public async Task<int> GetCommandsCount()
    {
        return await _context.Commands.CountAsync();
    }

    public async Task<ReadExtendedCommandDto> GetCommandById(int id, List<string>? expand = null)
    {
        var query = _context.Commands.AsQueryable();
        query = query.Where(c => c.id_command == id);
        var command = await query
            .Select(c => new
            {
                Command = c,
                CommandsCommentairesCount = c.CommandsCommentaires.Count,
                CommandsDocumentsCount = c.CommandsDocuments.Count,
                CommandsItemsCount = c.CommandsItems.Count,
                CommandsCommentaires = expand != null && expand.Contains("commands_commentaires") ? c.CommandsCommentaires.Take(20).ToList() : null,
                CommandsDocuments = expand != null && expand.Contains("commands_documents") ? c.CommandsDocuments.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("commands_items") ? c.CommandsItems.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Command with id {id} not found");
        return _mapper.Map<ReadExtendedCommandDto>(command.Command) with
        {
            commands_commentaires_count = command.CommandsCommentairesCount,
            commands_documents_count = command.CommandsDocumentsCount,
            commands_items_count = command.CommandsItemsCount,
            commands_commentaires = _mapper.Map<IEnumerable<ReadCommandCommentaireDto>>(command.CommandsCommentaires),
            commands_documents = _mapper.Map<IEnumerable<ReadCommandDocumentDto>>(command.CommandsDocuments),
            commands_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(command.CommandsItems)
        };
    }

    public async Task<ReadCommandDto> CreateCommand(CreateCommandDto commandDto)
    {
        var newCommand = _mapper.Map<Commands>(commandDto);
        // TODO went the format of status_command will be changed to enum
        // set date_livraison_command to null if status_command is enum for not delivered
        _context.Commands.Add(newCommand);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(_commandDocumentsPath, newCommand.id_command.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(_commandDocumentsPath, newCommand.id_command.ToString()));
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
        // TODO went the format of status_command will be changed to enum
        // set date_livraison_command to null if status_command is enum for not delivered
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDto>(commandToUpdate);
    }

    public async Task DeleteCommand(int id)
    {
        var commandToDelete = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id {id} not found");
        _context.Commands.Remove(commandToDelete);
        await _context.SaveChangesAsync();
        //remove folder in wwwroot/commandDocuments
        if (Directory.Exists(Path.Combine(_commandDocumentsPath, id.ToString())))
        {
            Directory.Delete(Path.Combine(_commandDocumentsPath, id.ToString()), true);
        }
    }
}
