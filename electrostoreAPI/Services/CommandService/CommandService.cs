using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.CommandService;

public class CommandService : ICommandService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _commandDocumentsPath = "commandDocuments";

    public CommandService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedCommandDto>> GetCommands(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var expandCommandComments = expand?.Contains("command_comments") == true;
        var expandCommandsDocuments = expand?.Contains("commands_documents") == true;
        var expandCommandsHistory = expand?.Contains("commands_history") == true;
        var expandCommandsItems = expand?.Contains("commands_items") == true;
        var expandCarrier = expand?.Contains("carrier") == true;
        var query = _context.Commands.AsQueryable();
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_command", order = "asc" })
            .ToProjectedResponseAsync(
                c => new
                {
                    Command = c,
                    CommandsCommentsCount = c.CommandsComments.Count,
                    CommandsDocumentsCount = c.CommandsDocuments.Count,
                    CommandsItemsCount = c.CommandsItems.Count,
                    CommandsComments = expandCommandComments ? c.CommandsComments.Take(20).ToList() : null,
                    CommandsDocuments = expandCommandsDocuments ? c.CommandsDocuments.Take(20).ToList() : null,
                    CommandsHistory = expandCommandsHistory ? c.CommandsHistory.Take(20).ToList() : null,
                    CommandsItems = expandCommandsItems ? c.CommandsItems.Take(20).ToList() : null,
                    Carrier = expandCarrier ? c.Carrier : null
                },
                command => command.Select(c => {
                    return _mapper.Map<ReadExtendedCommandDto>(c.Command) with
                    {
                        command_comments_count = c.CommandsCommentsCount,
                        commands_documents_count = c.CommandsDocumentsCount,
                        commands_items_count = c.CommandsItemsCount,
                        command_comments = _mapper.Map<IEnumerable<ReadCommandCommentDto>>(c.CommandsComments),
                        commands_documents = _mapper.Map<IEnumerable<ReadCommandDocumentDto>>(c.CommandsDocuments),
                        commands_history = _mapper.Map<IEnumerable<ReadCommandHistoryDto>>(c.CommandsHistory),
                        commands_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(c.CommandsItems),
                        carrier = c.Command.Carrier != null ? _mapper.Map<ReadCarrierDto>(c.Command.Carrier) : null
                    };
                }).ToList());
    }

    public async Task<ReadExtendedCommandDto> GetCommandById(int id, List<string>? expand = null)
    {
        var expandCommandComments = expand?.Contains("command_comments") == true;
        var expandCommandsDocuments = expand?.Contains("commands_documents") == true;
        var expandCommandsHistory = expand?.Contains("commands_history") == true;
        var expandCommandsItems = expand?.Contains("commands_items") == true;
        var expandCarrier = expand?.Contains("carrier") == true;
        var query = _context.Commands.AsQueryable();
        query = query.Where(c => c.id_command == id);
        var command = await query
            .Select(c => new
            {
                Command = c,
                CommandsCommentsCount = c.CommandsComments.Count,
                CommandsDocumentsCount = c.CommandsDocuments.Count,
                CommandsItemsCount = c.CommandsItems.Count,
                CommandsComments = expandCommandComments ? c.CommandsComments.Take(20).ToList() : null,
                CommandsDocuments = expandCommandsDocuments ? c.CommandsDocuments.Take(20).ToList() : null,
                CommandsHistory = expandCommandsHistory ? c.CommandsHistory.Take(20).ToList() : null,
                CommandsItems = expandCommandsItems ? c.CommandsItems.Take(20).ToList() : null,
                Carrier = expandCarrier ? c.Carrier : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Command with id '{id}' not found");
        return _mapper.Map<ReadExtendedCommandDto>(command.Command) with
        {
            command_comments_count = command.CommandsCommentsCount,
            commands_documents_count = command.CommandsDocumentsCount,
            commands_items_count = command.CommandsItemsCount,
            command_comments = _mapper.Map<IEnumerable<ReadCommandCommentDto>>(command.CommandsComments),
            commands_documents = _mapper.Map<IEnumerable<ReadCommandDocumentDto>>(command.CommandsDocuments),
            commands_history = _mapper.Map<IEnumerable<ReadCommandHistoryDto>>(command.CommandsHistory),
            commands_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(command.CommandsItems),
            carrier = command.Command.Carrier != null ? _mapper.Map<ReadCarrierDto>(command.Command.Carrier) : null
        };
    }

    public async Task<ReadCommandDto> CreateCommand(CreateCommandDto commandDto)
    {
        var newCommand = _mapper.Map<Commands>(commandDto);
        if (commandDto.id_carrier is not null)
        {
            _ = await _context.Carriers.FindAsync(commandDto.id_carrier.Value) ?? throw new KeyNotFoundException($"Carrier with id '{commandDto.id_carrier}' not found");
        }
        _context.Commands.Add(newCommand);
        await _fileService.CreateDirectory(Path.Combine(_commandDocumentsPath, newCommand.id_command.ToString()));
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDto>(newCommand);
    }

    public async Task<ReadCommandDto> UpdateCommand(int id, UpdateCommandDto commandDto)
    {
        var commandToUpdate = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id '{id}' not found");
        if (commandDto.price_command is not null)
        {
            commandToUpdate.price_command = commandDto.price_command.Value;
        }
        if (commandDto.url_command is not null)
        {
            commandToUpdate.url_command = commandDto.url_command;
        }
        if (commandDto.status_command is not null)
        {
            commandToUpdate.status_command = commandDto.status_command.Value;
        }
        if (commandDto.date_command is not null)
        {
            commandToUpdate.date_command = commandDto.date_command.Value;
        }
        if (commandDto.date_delivery_command is not null)
        {
            commandToUpdate.date_delivery_command = commandDto.date_delivery_command;
        }
        if (commandDto.tracking_number_command is not null)
        {
            commandToUpdate.tracking_number_command = commandDto.tracking_number_command;
        }
        if (commandDto.id_carrier is not null)
        {
            _ = await _context.Carriers.FindAsync(commandDto.id_carrier.Value) ?? throw new KeyNotFoundException($"Carrier with id '{commandDto.id_carrier}' not found");
            commandToUpdate.id_carrier = commandDto.id_carrier.Value;
        }
        if (commandDto.is_tracking_requested is not null)
        {
            commandToUpdate.is_tracking_requested = commandDto.is_tracking_requested.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDto>(commandToUpdate);
    }

    public async Task DeleteCommand(int id)
    {
        var commandToDelete = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id '{id}' not found");
        _context.Commands.Remove(commandToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_commandDocumentsPath, id.ToString()));
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCommandStatusByTracking(string trackingNumber, int carrierKey, string action)
    {
        var commands = await _context.Commands
            .Include(c => c.Carrier)
            .Where(c => c.tracking_number_command == trackingNumber
                     && c.Carrier != null
                     && c.Carrier.key_carrier == carrierKey)
            .ToListAsync();

        if (commands.Count == 0) 
        {
            return; // No commands found for the given tracking number and carrier key
        }

        foreach (var command in commands)
        {
            switch (action)
            {
                case "register":
                    command.is_tracking_validated = true;
                    command.is_active = true;
                    break;
                case "stoptrack":
                    command.is_active = false;
                    break;
                case "retrack":
                    command.is_active = true;
                    break;
                case "deletetrack":
                    command.is_tracking_validated = false;
                    command.is_active = false;
                    break;
            }
        }

        await _context.SaveChangesAsync();
    }
}
