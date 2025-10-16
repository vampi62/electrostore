using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandDocumentService;

public class CommandDocumentService : ICommandDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly FileService.FileService _fileService;
    private readonly string _commandDocumentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments");

    public CommandDocumentService(IMapper mapper, ApplicationDbContext context, FileService.FileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<IEnumerable<ReadCommandDocumentDto>> GetCommandsDocumentsByCommandId(int commandId, int limit = 100, int offset = 0)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        var query = _context.CommandsDocuments.AsQueryable();
        query = query.Where(cd => cd.id_command == commandId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(cd => cd.id_command_document);
        var commandDocument = await query.ToListAsync();
        return _mapper.Map<List<ReadCommandDocumentDto>>(commandDocument);
    }

    public async Task<int> GetCommandsDocumentsCountByCommandId(int commandId)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        return await _context.CommandsDocuments
            .Where(cd => cd.id_command == commandId)
            .CountAsync();
    }

    public async Task<ReadCommandDocumentDto> GetCommandDocumentById(int id, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id {id} not found");
        if (commandId is not null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id {id} not found for command with id {commandId}");
        }
        return _mapper.Map<ReadCommandDocumentDto>(commandDocument);
    }

    public async Task<ReadCommandDocumentDto> CreateCommandDocument(CreateCommandDocumentDto commandDocumentDto)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commandDocumentDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id {commandDocumentDto.id_command} not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_commandDocumentsPath, commandDocumentDto.id_command.ToString()), commandDocumentDto.document);
        var commandDocument = new CommandsDocuments
        {
            id_command = commandDocumentDto.id_command,
            url_command_document = savedFile.url,
            name_command_document = commandDocumentDto.name_command_document,
            type_command_document = savedFile.mimeType,
            size_command_document = commandDocumentDto.document.Length
        };
        await _context.CommandsDocuments.AddAsync(commandDocument);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDocumentDto>(commandDocument);
    }

    public async Task<ReadCommandDocumentDto> UpdateCommandDocument(int id, UpdateCommandDocumentDto commandDocumentDto, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id {id} not found");
        if (commandId is not null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id {id} not found for command with id {commandId}");
        }
        if (commandDocumentDto.name_command_document is not null)
        {
            commandDocument.name_command_document = commandDocumentDto.name_command_document;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadCommandDocumentDto>(commandDocument);
    }

    public async Task DeleteCommandDocument(int id, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id {id} not found");
        if (commandId is not null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id {id} not found for command with id {commandId}");
        }
        await _fileService.DeleteFile(commandDocument.url_command_document);
        _context.CommandsDocuments.Remove(commandDocument);
        await _context.SaveChangesAsync();
    }
}