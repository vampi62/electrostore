using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Kafka.Messages;
using ElectrostoreAPI.Kafka.Producer;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.CommandHistoryService;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Text.Json;

namespace ElectrostoreAPI.Services.CommandService;

public class CommandService : ICommandService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly IKafkaProducerService _kafkaProducerService;
    private readonly ICommandHistoryService _commandHistoryService;
    private readonly string _commandDocumentsPath = "commandDocuments";

    public CommandService(IMapper mapper, ApplicationDbContext context, IFileService fileService, IKafkaProducerService kafkaProducerService, ICommandHistoryService commandHistoryService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
        _kafkaProducerService = kafkaProducerService;
        _commandHistoryService = commandHistoryService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedCommandDto>> GetCommands(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Commands.AsQueryable();
        var filterResult = default(Expression<Func<Commands, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(c => idResearch.Contains(c.id_command));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Commands>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Commands>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_command", Order = "asc" };
                    query = query.OrderBy(c => c.id_command);
                }
            }
            else
            {
                query = query.OrderBy(c => c.id_command);
            }
        }
        query = query.Skip(offset).Take(limit);
        var command = await query
            .Select(c => new
            {
                Command = c,
                CommandsCommentairesCount = c.CommandsCommentaires.Count,
                CommandsDocumentsCount = c.CommandsDocuments.Count,
                CommandsItemsCount = c.CommandsItems.Count,
                CommandsCommentaires = expand != null && expand.Contains("commands_commentaires") ? c.CommandsCommentaires.Take(20).ToList() : null,
                CommandsDocuments = expand != null && expand.Contains("commands_documents") ? c.CommandsDocuments.Take(20).ToList() : null,
                CommandsHistory = expand != null && expand.Contains("commands_history") ? c.CommandsHistory.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("commands_items") ? c.CommandsItems.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedCommandDto>
        {
            data = command.Select(c => {
                return _mapper.Map<ReadExtendedCommandDto>(c.Command) with
                {
                    commands_commentaires_count = c.CommandsCommentairesCount,
                    commands_documents_count = c.CommandsDocumentsCount,
                    commands_items_count = c.CommandsItemsCount,
                    commands_commentaires = _mapper.Map<IEnumerable<ReadCommandCommentaireDto>>(c.CommandsCommentaires),
                    commands_documents = _mapper.Map<IEnumerable<ReadCommandDocumentDto>>(c.CommandsDocuments),
                    commands_history = _mapper.Map<IEnumerable<ReadCommandHistoryDto>>(c.CommandsHistory),
                    commands_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(c.CommandsItems)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Commands.CountAsync(filterResult ?? (c => true)),
                nextOffset = offset + limit,
                hasMore = await _context.Commands.Skip(offset + limit).AnyAsync(filterResult ?? (c => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
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
                CommandsHistory = expand != null && expand.Contains("commands_history") ? c.CommandsHistory.Take(20).ToList() : null,
                CommandsItems = expand != null && expand.Contains("commands_items") ? c.CommandsItems.Take(20).ToList() : null,
                Carrier = expand != null && expand.Contains("carrier") ? c.Carrier : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Command with id '{id}' not found");
        return _mapper.Map<ReadExtendedCommandDto>(command.Command) with
        {
            commands_commentaires_count = command.CommandsCommentairesCount,
            commands_documents_count = command.CommandsDocumentsCount,
            commands_items_count = command.CommandsItemsCount,
            commands_commentaires = _mapper.Map<IEnumerable<ReadCommandCommentaireDto>>(command.CommandsCommentaires),
            commands_documents = _mapper.Map<IEnumerable<ReadCommandDocumentDto>>(command.CommandsDocuments),
            commands_history = _mapper.Map<IEnumerable<ReadCommandHistoryDto>>(command.CommandsHistory),
            commands_items = _mapper.Map<IEnumerable<ReadCommandItemDto>>(command.CommandsItems),
            carrier = command.Command.Carrier != null ? _mapper.Map<ReadCarrierDto>(command.Command.Carrier) : null
        };
    }

    public async Task<ReadCommandDto> CreateCommand(CreateCommandDto commandDto)
    {
        var newCommand = _mapper.Map<Commands>(commandDto);
        var carrier = default(Carriers);
        if (commandDto.id_carrier is not null)
        {
            carrier = await _context.Carriers.FindAsync(commandDto.id_carrier.Value) ?? throw new KeyNotFoundException($"Carrier with id '{commandDto.id_carrier}' not found");
        }
        _context.Commands.Add(newCommand);
        await _fileService.CreateDirectory(Path.Combine(_commandDocumentsPath, newCommand.id_command.ToString()));
        await _context.SaveChangesAsync();
        if (!string.IsNullOrEmpty(newCommand.tracking_number) && newCommand.id_carrier != 0 && newCommand.is_tracking_requested)
        {
            var carrierKey = carrier?.key ?? 0;
            var kafkaKey = $"{newCommand.tracking_number}_{carrierKey}";
            var message = new TrackingActionMessage
            {
                tracking_number = newCommand.tracking_number,
                carrier = carrierKey
            };
            var messageJson = JsonSerializer.Serialize(message);
            await _kafkaProducerService.PublishAsync("tracking-request-add", kafkaKey, messageJson);
        }
        return _mapper.Map<ReadCommandDto>(newCommand);
    }

    public async Task<ReadCommandDto> UpdateCommand(int id, UpdateCommandDto commandDto)
    {
        var commandToUpdate = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id '{id}' not found");
        var oldTrackingNumber = commandToUpdate.tracking_number;
        var oldCarrierId = commandToUpdate.id_carrier;
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
            commandToUpdate.status_command = commandDto.status_command.Value;
        }
        if (commandDto.date_command is not null)
        {
            commandToUpdate.date_command = commandDto.date_command.Value;
        }
        if (commandDto.date_livraison_command is not null)
        {
            commandToUpdate.date_livraison_command = commandDto.date_livraison_command;
        }
        if (commandDto.tracking_number is not null)
        {
            commandToUpdate.tracking_number = commandDto.tracking_number;
        }
        if (commandDto.id_carrier is not null)
        {
            var carrier = await _context.Carriers.FindAsync(commandDto.id_carrier.Value) ?? throw new KeyNotFoundException($"Carrier with id '{commandDto.id_carrier}' not found");
            commandToUpdate.id_carrier = commandDto.id_carrier.Value;
        }
        if (commandDto.is_tracking_requested is not null)
        {
            commandToUpdate.is_tracking_requested = commandDto.is_tracking_requested.Value;
        }
        await _context.SaveChangesAsync();
        //tracking-request-add si is_tracking_requested && !is_tracking_validated
        if (!string.IsNullOrEmpty(commandToUpdate.tracking_number) && commandToUpdate.id_carrier != 0 &&
            ((commandDto.is_tracking_requested is not null && !commandToUpdate.is_tracking_validated && commandDto.is_tracking_requested.Value) ||
            (!string.IsNullOrEmpty(commandDto.tracking_number) && commandDto.tracking_number != oldTrackingNumber)))
        {
            var carrierEntity = await _context.Carriers.FindAsync(commandToUpdate.id_carrier) ?? throw new KeyNotFoundException($"Carrier with id '{commandToUpdate.id_carrier}' not found");
            var carrierKey = carrierEntity?.key ?? 0;
            var kafkaKey = $"{commandToUpdate.tracking_number}_{carrierKey}";
            var message = new TrackingActionMessage
            {
                tracking_number = commandToUpdate.tracking_number,
                carrier = carrierKey
            };
            var messageJson = JsonSerializer.Serialize(message);
            await _kafkaProducerService.PublishAsync("tracking-request-add", kafkaKey, messageJson);
        }
        //tracking-request-change si is_tracking_validated && id_carrier !=
        if (commandDto.id_carrier is not null && commandToUpdate.id_carrier != 0 && commandToUpdate.is_tracking_validated && commandDto.id_carrier.Value != oldCarrierId)
        {
            var newCarrierEntity = await _context.Carriers.FindAsync(commandDto.id_carrier) ?? throw new KeyNotFoundException($"Carrier with id '{commandDto.id_carrier}' not found");
            var oldCarrierEntity = await _context.Carriers.FindAsync(oldCarrierId) ?? throw new KeyNotFoundException($"Carrier with id '{oldCarrierId}' not found");
            var newCarrierKey = newCarrierEntity?.key ?? 0;
            var oldCarrierKey = oldCarrierEntity?.key ?? 0;
            var kafkaKey = $"{commandToUpdate.tracking_number}_{newCarrierKey}";
            var message = new TrackingActionMessage
            {
                tracking_number = commandToUpdate.tracking_number,
                carrier = newCarrierKey,
                carrier_old = oldCarrierKey
            };
            var messageJson = JsonSerializer.Serialize(message);
            await _kafkaProducerService.PublishAsync("tracking-request-change", kafkaKey, messageJson);
        }
        //tracking-request-stop si !is_tracking_requested && is_tracking_validated && is_active
        //tracking-request-resume si is_tracking_requested && is_tracking_validated && !is_active
        if (commandDto.is_tracking_requested is not null && commandToUpdate.is_tracking_validated && commandToUpdate.is_active && !commandDto.is_tracking_requested.Value)
        {
            var carrierEntity = await _context.Carriers.FindAsync(commandToUpdate.id_carrier) ?? throw new KeyNotFoundException($"Carrier with id '{commandToUpdate.id_carrier}' not found");
            var carrierKey = carrierEntity?.key ?? 0;
            var kafkaKey = $"{commandToUpdate.tracking_number}_{carrierKey}";
            var message = new TrackingActionMessage
            {
                tracking_number = commandToUpdate.tracking_number,
                carrier = carrierKey
            };
            var messageJson = JsonSerializer.Serialize(message);
            await _kafkaProducerService.PublishAsync("tracking-request-stop", kafkaKey, messageJson);
        }
        if (commandDto.is_tracking_requested is not null && commandToUpdate.is_tracking_validated && !commandToUpdate.is_active && commandDto.is_tracking_requested.Value)
        {
            var carrierEntity = await _context.Carriers.FindAsync(commandToUpdate.id_carrier) ?? throw new KeyNotFoundException($"Carrier with id '{commandToUpdate.id_carrier}' not found");
            var carrierKey = carrierEntity?.key ?? 0;
            var kafkaKey = $"{commandToUpdate.tracking_number}_{carrierKey}";
            var message = new TrackingActionMessage
            {
                tracking_number = commandToUpdate.tracking_number,
                carrier = carrierKey
            };
            var messageJson = JsonSerializer.Serialize(message);
            await _kafkaProducerService.PublishAsync("tracking-request-resume", kafkaKey, messageJson);
        }
        //tracking-request-delete si tracking_number !=
        if (commandDto.tracking_number is not null && commandToUpdate.is_tracking_validated && !string.IsNullOrEmpty(oldTrackingNumber) && commandDto.tracking_number != oldTrackingNumber)
        {
            var carrierEntity = await _context.Carriers.FindAsync(commandToUpdate.id_carrier) ?? throw new KeyNotFoundException($"Carrier with id '{commandToUpdate.id_carrier}' not found");
            var carrierKey = carrierEntity?.key ?? 0;
            var kafkaKey = $"{commandToUpdate.tracking_number}_{carrierKey}";
            var message = new TrackingActionMessage
            {
                tracking_number = commandToUpdate.tracking_number,
                carrier = carrierKey
            };
            var messageJson = JsonSerializer.Serialize(message);
            await _kafkaProducerService.PublishAsync("tracking-request-delete", kafkaKey, messageJson);
        }
        return _mapper.Map<ReadCommandDto>(commandToUpdate);
    }

    public async Task DeleteCommand(int id)
    {
        var commandToDelete = await _context.Commands.FindAsync(id) ?? throw new KeyNotFoundException($"Command with id '{id}' not found");
        _context.Commands.Remove(commandToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_commandDocumentsPath, id.ToString()));
        if (!string.IsNullOrEmpty(commandToDelete.tracking_number) && commandToDelete.id_carrier != 0 && commandToDelete.is_tracking_validated && commandToDelete.is_active)
        {
            var carrierEntity = await _context.Carriers.FindAsync(commandToDelete.id_carrier);
            var carrierKey = carrierEntity?.key ?? 0;
            var kafkaKey = $"{commandToDelete.tracking_number}_{carrierKey}";
            var message = new TrackingActionMessage
            {
                tracking_number = commandToDelete.tracking_number,
                carrier = carrierKey
            };
            var messageJson = JsonSerializer.Serialize(message);
            await _kafkaProducerService.PublishAsync("tracking-request-delete", kafkaKey, messageJson);
        }
        await _context.SaveChangesAsync();
    }

    public async Task UpdateCommandStatusByTracking(string trackingNumber, int carrierKey, string action)
    {
        var commands = await _context.Commands
            .Include(c => c.Carrier)
            .Where(c => c.tracking_number == trackingNumber
                     && c.Carrier != null
                     && c.Carrier.key == carrierKey)
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
