using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandDocumentService;

public class CommandDocumentService : ICommandDocumentService
{
    private readonly ApplicationDbContext _context;

    public CommandDocumentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadCommandDocumentDto>> GetCommandDocumentsByCommandId(int commandId, int limit = 100, int offset = 0)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(command => command.id_command == commandId))
        {
            throw new KeyNotFoundException($"Command with id {commandId} not found");
        }
        return await _context.CommandsDocuments
            .Where(id => id.id_command == commandId)
            .Skip(offset)
            .Take(limit)
            .Select(commandDocument => new ReadCommandDocumentDto
            {
                id_command_document = commandDocument.id_command_document,
                id_command = commandDocument.id_command,
                url_command_document = commandDocument.url_command_document,
                name_command_document = commandDocument.name_command_document,
                type_command_document = commandDocument.type_command_document,
                size_command_document = commandDocument.size_command_document,
                date_command_document = commandDocument.date_command_document
            })
            .ToListAsync();
    }

    public async Task<int> GetCommandDocumentsCountByCommandId(int commandId)
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
        if (commandId != null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id {id} not found for command with id {commandId}");
        }
        return new ReadCommandDocumentDto
        {
                id_command_document = commandDocument.id_command_document,
                id_command = commandDocument.id_command,
                url_command_document = commandDocument.url_command_document,
                name_command_document = commandDocument.name_command_document,
                type_command_document = commandDocument.type_command_document,
                size_command_document = commandDocument.size_command_document,
                date_command_document = commandDocument.date_command_document
        };
    }

    public async Task<ReadCommandDocumentDto> CreateCommandDocument(CreateCommandDocumentDto commandDocumentDto)
    {
        // check if command exists
        if (!await _context.Commands.AnyAsync(command => command.id_command == commandDocumentDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id {commandDocumentDto.id_command} not found");
        }
        if (commandDocumentDto.document == null || commandDocumentDto.document.Length == 0)
        {
            throw new ArgumentException("Image file is required");
        }
        if (commandDocumentDto.document.Length > (30 * 1024 * 1024)) // 30MB max
        {
            throw new ArgumentException("Image file size should not exceed 30MB");
        }
        var fileName = Path.GetFileNameWithoutExtension(commandDocumentDto.document.FileName);
        var fileExt = Path.GetExtension(commandDocumentDto.document.FileName);
        if (!new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" }.Contains(fileExt)) // if extension is not allowed
        {
            throw new ArgumentException("Document file extension not allowed");
        }
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
            url_command_document = commandDocumentDto.id_command + "/" + newName,
            name_command_document = commandDocumentDto.name_command_document,
            type_command_document = commandDocumentDto.type_command_document,
            size_command_document = commandDocumentDto.document.Length,
            date_command_document = DateTime.Now
        };
        await _context.CommandsDocuments.AddAsync(commandDocument);
        await _context.SaveChangesAsync();
        return new ReadCommandDocumentDto
        {
            id_command_document = commandDocument.id_command_document,
            id_command = commandDocument.id_command,
            url_command_document = commandDocument.url_command_document,
            name_command_document = commandDocument.name_command_document,
            type_command_document = commandDocument.type_command_document,
            size_command_document = commandDocument.size_command_document,
            date_command_document = commandDocument.date_command_document
        };
    }

    public async Task<ReadCommandDocumentDto> UpdateCommandDocument(int id, UpdateCommandDocumentDto commandDocumentDto, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id {id} not found");
        if (commandId != null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id {id} not found for command with id {commandId}");
        }
        if (commandDocumentDto.document != null && commandDocumentDto.document.Length > 0)
        {
            var oldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocument.id_command.ToString(), commandDocument.url_command_document);
            if (commandDocumentDto.document.Length == 0)
            {
                throw new ArgumentException("Document file is required");
            }
            if (commandDocumentDto.document.Length > (30 * 1024 * 1024)) // 30MB max
            {
                throw new ArgumentException("Image file size should not exceed 30MB");
            }
            var fileName = Path.GetFileNameWithoutExtension(commandDocumentDto.document.FileName);
            var fileExt = Path.GetExtension(commandDocumentDto.document.FileName);
            if (!new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".png", ".jpg", ".jpeg", ".gif", ".bmp" }.Contains(fileExt)) // if extension is not allowed
            {
                throw new ArgumentException("Document file extension not allowed");
            }
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
            commandDocument.url_command_document = commandDocument.id_command + "/" + newName;
            commandDocument.size_command_document = commandDocumentDto.document.Length;
            commandDocument.date_command_document = DateTime.Now;
            // remove old file
            if (File.Exists(oldPath))
            {
                File.Delete(oldPath);
            }
        }
        if (commandDocumentDto.name_command_document != null)
        {
            commandDocument.name_command_document = commandDocumentDto.name_command_document;
        }
        if (commandDocumentDto.type_command_document != null)
        {
            commandDocument.type_command_document = commandDocumentDto.type_command_document;
        }
        await _context.SaveChangesAsync();
        return new ReadCommandDocumentDto
        {
            id_command_document = commandDocument.id_command_document,
            id_command = commandDocument.id_command,
            url_command_document = commandDocument.url_command_document,
            name_command_document = commandDocument.name_command_document,
            type_command_document = commandDocument.type_command_document,
            size_command_document = commandDocument.size_command_document,
            date_command_document = commandDocument.date_command_document
        };
    }

    public async Task DeleteCommandDocument(int id, int? commandId = null)
    {
        var commandDocument = await _context.CommandsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"CommandDocument with id {id} not found");
        if (commandId != null && commandDocument.id_command != commandId)
        {
            throw new KeyNotFoundException($"CommandDocument with id {id} not found for command with id {commandId}");
        }
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/commandDocuments", commandDocument.id_command.ToString(), commandDocument.url_command_document);
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