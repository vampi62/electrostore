using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.CommandDocumentService;

public class CommandDocumentService : ICommandDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public CommandDocumentService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadCommandDocumentDto>> GetCommandsDocumentsByCommandId(int commandId, int limit = 100, int offset = 0)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(command => command.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        var query = _context.CommandsDocuments.AsQueryable();
        query = query.Where(commandDocument => commandDocument.id_command == commandId);
        query = query.Skip(offset).Take(limit);
        var commandDocument = await query.ToListAsync();
        return _mapper.Map<List<ReadCommandDocumentDto>>(commandDocument);
    }

    public async Task<int> GetCommandsDocumentsCountByCommandId(int commandId)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(command => command.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        return await _context.CommandsDocuments
            .Where(id => id.id_command == commandId)
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
        if (!await _context.Commands.AnyAsync(command => command.id_command == commandDocumentDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id {commandDocumentDto.id_command} not found");
        }
        var fileName = Path.GetFileNameWithoutExtension(commandDocumentDto.document.FileName);
        var fileExt = Path.GetExtension(commandDocumentDto.document.FileName);
        var i = 1;
        // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/commandDocuments"
        // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
        var newName = commandDocumentDto.document.FileName;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocumentDto.id_command.ToString(), newName)))
        {
            newName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocumentDto.id_command.ToString(), newName);
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
        if (commandDocumentDto.document is not null)
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocument.url_command_document);
            var fileName = Path.GetFileNameWithoutExtension(commandDocumentDto.document.FileName);
            var fileExt = Path.GetExtension(commandDocumentDto.document.FileName);
            var i = 1;
            // verifie si un document avec le meme nom existe deja sur le serveur dans "wwwroot/commandDocuments"
            // si oui, on ajoute un numero a la fin du nom du document et on recommence la verification jusqu'a trouver un nom disponible
            var newName = commandDocumentDto.document.FileName;
            while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocument.id_command.ToString(), newName)))
            {
                newName = $"{fileName}({i}){fileExt}";
                i++;
            }
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocument.id_command.ToString(), newName);
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await commandDocumentDto.document.CopyToAsync(fileStream);
            }
            commandDocument.type_command_document = fileExt.Replace(".", "").ToLowerInvariant();
            commandDocument.url_command_document = commandDocument.id_command.ToString() + "/" + newName;
            commandDocument.size_command_document = commandDocumentDto.document.Length;
            // remove old file
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
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
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocument.url_command_document);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        _context.CommandsDocuments.Remove(commandDocument);
        await _context.SaveChangesAsync();
    }

    public async Task<GetFileResult> GetFile(string url)
    {
        var pathImg = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", url);
        if (!File.Exists(pathImg))
        {
            return new GetFileResult
            {
                Success = false,
                ErrorMessage = "File not found",
                FilePath = "",
                MimeType = ""
            };
        } else {
            var ext = Path.GetExtension(pathImg);
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".txt" => "text/plain",
                _ => "application/octet-stream"
            };
            return await Task.FromResult(new GetFileResult
            {
                Success = true,
                FilePath = pathImg,
                MimeType = mimeType
            });
        }
    }
}