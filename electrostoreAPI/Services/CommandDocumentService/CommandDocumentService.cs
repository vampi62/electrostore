using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandDocumentService;

public class CommandDocumentService : ICommandDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly string _commandDocumentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments");

    public CommandDocumentService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
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
        var fileName = Path.GetFileNameWithoutExtension(commandDocumentDto.document.FileName);
        fileName = fileName.Replace(".", "").Replace("/", ""); // remove "." and "/" from the file name to prevent directory traversal attacks
        if (fileName.Length > 100) // cut the file name to 100 characters to prevent too long file names
        {
            fileName = fileName[..100];
        }
        var fileExt = Path.GetExtension(commandDocumentDto.document.FileName);
        var i = 1;
        // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/commandDocuments"
        // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
        var newName = fileName;
        while (File.Exists(Path.Combine(_commandDocumentsPath, commandDocumentDto.id_command.ToString(), newName)))
        {
            newName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var savePath = Path.Combine(_commandDocumentsPath, commandDocumentDto.id_command.ToString(), newName);
        using (var fileStream = new FileStream(savePath, FileMode.Create))
        {
            await commandDocumentDto.document.CopyToAsync(fileStream);
        }
        var commandDocument = new CommandsDocuments
        {
            id_command = commandDocumentDto.id_command,
            url_command_document = commandDocumentDto.id_command.ToString() + "/" + newName,
            name_command_document = commandDocumentDto.name_command_document,
            type_command_document = fileExt.Replace(".", "").ToLowerInvariant(),
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
        var path = Path.Combine(_commandDocumentsPath, commandDocument.url_command_document);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        _context.CommandsDocuments.Remove(commandDocument);
        await _context.SaveChangesAsync();
    }
}