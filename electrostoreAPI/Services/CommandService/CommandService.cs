using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandService;

public class CommandService : ICommandService
{
    private readonly ApplicationDbContext _context;

    public CommandService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedCommandDto>> GetCommands(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Commands.AsQueryable();
        if (idResearch != null && idResearch.Any())
        {
            query = query.Where(b => idResearch.Contains(b.id_command));
        }
        return await query
            .Skip(offset)
            .Take(limit)
            .Select(c => new ReadExtendedCommandDto
            {
                id_command = c.id_command,
                prix_command = c.prix_command,
                url_command = c.url_command,
                status_command = c.status_command,
                date_command = c.date_command,
                date_livraison_command = c.date_livraison_command,
                commands_commentaires = expand != null && expand.Contains("commands_commentaires") ? c.CommandsCommentaires.Select(cc => new ReadCommandCommentaireDto
                {
                    id_command_commentaire = cc.id_command_commentaire,
                    id_command = cc.id_command,
                    id_user = cc.id_user,
                    contenu_command_commentaire = cc.contenu_command_commentaire,
                    date_command_commentaire = cc.date_command_commentaire,
                    date_modif_command_commentaire = cc.date_modif_command_commentaire
                }).ToList() : null,
                commands_documents = expand != null && expand.Contains("commands_documents") ? c.CommandsDocuments.Select(cd => new ReadCommandDocumentDto
                {
                    id_command_document = cd.id_command_document,
                    id_command = cd.id_command,
                    url_command_document = cd.url_command_document,
                    name_command_document = cd.name_command_document,
                    type_command_document = cd.type_command_document,
                    size_command_document = cd.size_command_document,
                    date_command_document = cd.date_command_document
                }).ToList() : null,
                commands_items = expand != null && expand.Contains("commands_items") ? c.CommandsItems.Select(ci => new ReadCommandItemDto
                {
                    id_item = ci.id_item,
                    id_command = ci.id_command,
                    qte_command_item = ci.qte_command_item,
                    prix_command_item = ci.prix_command_item
                }).ToList() : null,
                commands_commentaires_count = c.CommandsCommentaires.Count,
                commands_documents_count = c.CommandsDocuments.Count,
                commands_items_count = c.CommandsItems.Count
            })
            .ToListAsync();
    }

    public async Task<int> GetCommandsCount()
    {
        return await _context.Commands.CountAsync();
    }

    public async Task<ReadExtendedCommandDto> GetCommandById(int id, List<string>? expand = null)
    {
        return await _context.Commands
            .Where(c => c.id_command == id)
            .Select(c => new ReadExtendedCommandDto
            {
                id_command = c.id_command,
                prix_command = c.prix_command,
                url_command = c.url_command,
                status_command = c.status_command,
                date_command = c.date_command,
                date_livraison_command = c.date_livraison_command,
                commands_commentaires = expand != null && expand.Contains("commands_commentaires") ? c.CommandsCommentaires.Select(cc => new ReadCommandCommentaireDto
                {
                    id_command_commentaire = cc.id_command_commentaire,
                    id_command = cc.id_command,
                    id_user = cc.id_user,
                    contenu_command_commentaire = cc.contenu_command_commentaire,
                    date_command_commentaire = cc.date_command_commentaire,
                    date_modif_command_commentaire = cc.date_modif_command_commentaire
                }).ToList() : null,
                commands_documents = expand != null && expand.Contains("commands_documents") ? c.CommandsDocuments.Select(cd => new ReadCommandDocumentDto
                {
                    id_command_document = cd.id_command_document,
                    id_command = cd.id_command,
                    url_command_document = cd.url_command_document,
                    name_command_document = cd.name_command_document,
                    type_command_document = cd.type_command_document,
                    size_command_document = cd.size_command_document,
                    date_command_document = cd.date_command_document
                }).ToList() : null,
                commands_items = expand != null && expand.Contains("commands_items") ? c.CommandsItems.Select(ci => new ReadCommandItemDto
                {
                    id_item = ci.id_item,
                    id_command = ci.id_command,
                    qte_command_item = ci.qte_command_item,
                    prix_command_item = ci.prix_command_item
                }).ToList() : null,
                commands_commentaires_count = c.CommandsCommentaires.Count,
                commands_documents_count = c.CommandsDocuments.Count,
                commands_items_count = c.CommandsItems.Count
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Command with id {id} not found");
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
        return new ReadCommandDto
        {
            id_command = newCommand.id_command,
            prix_command = newCommand.prix_command,
            url_command = newCommand.url_command,
            status_command = newCommand.status_command,
            date_command = newCommand.date_command,
            date_livraison_command = newCommand.date_livraison_command
        };
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
        return new ReadCommandDto
        {
            id_command = commandToUpdate.id_command,
            prix_command = commandToUpdate.prix_command,
            url_command = commandToUpdate.url_command,
            status_command = commandToUpdate.status_command,
            date_command = commandToUpdate.date_command,
            date_livraison_command = commandToUpdate.date_livraison_command
        };
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
