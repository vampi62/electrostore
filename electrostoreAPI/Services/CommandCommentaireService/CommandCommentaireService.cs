using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandCommentaireService;

public class CommandCommentaireService : ICommandCommentaireService
{
    private readonly ApplicationDbContext _context;

    public CommandCommentaireService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(p => p.id_command == CommandId))
        {
            throw new KeyNotFoundException($"Command with id {CommandId} not found");
        }
        return await _context.CommandsCommentaires
            .Where(c => c.id_command == CommandId)
            .Skip(offset)
            .Take(limit)
            .Select(c => new ReadExtendedCommandCommentaireDto
            {
                id_command_commentaire = c.id_command_commentaire,
                id_command = c.id_command,
                id_user = c.id_user,
                contenu_command_commentaire = c.contenu_command_commentaire,
                date_command_commentaire = c.date_command_commentaire,
                date_modif_command_commentaire = c.date_modif_command_commentaire,
                command = expand != null && expand.Contains("command") ? new ReadCommandDto
                {
                    id_command = c.Command.id_command,
                    prix_command = c.Command.prix_command,
                    url_command = c.Command.url_command,
                    status_command = c.Command.status_command,
                    date_command = c.Command.date_command,
                    date_livraison_command = c.Command.date_livraison_command
                } : null,
                user = expand != null && expand.Contains("user") ? new ReadUserDto
                {
                    id_user = c.User.id_user,
                    nom_user = c.User.nom_user,
                    prenom_user = c.User.prenom_user,
                    email_user = c.User.email_user,
                    role_user = c.User.role_user
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetCommandsCommentairesCountByCommandId(int CommandId)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(p => p.id_command == CommandId))
        {
            throw new KeyNotFoundException($"Command with id {CommandId} not found");
        }
        return await _context.CommandsCommentaires
            .Where(c => c.id_command == CommandId)
            .CountAsync();
    }

    public async Task<IEnumerable<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.CommandsCommentaires
            .Where(c => c.id_user == userId)
            .Skip(offset)
            .Take(limit)
            .Select(c => new ReadExtendedCommandCommentaireDto
            {
                id_command_commentaire = c.id_command_commentaire,
                id_command = c.id_command,
                id_user = c.id_user,
                contenu_command_commentaire = c.contenu_command_commentaire,
                date_command_commentaire = c.date_command_commentaire,
                date_modif_command_commentaire = c.date_modif_command_commentaire,
                command = expand != null && expand.Contains("command") ? new ReadCommandDto
                {
                    id_command = c.Command.id_command,
                    prix_command = c.Command.prix_command,
                    url_command = c.Command.url_command,
                    status_command = c.Command.status_command,
                    date_command = c.Command.date_command,
                    date_livraison_command = c.Command.date_livraison_command
                } : null,
                user = expand != null && expand.Contains("user") ? new ReadUserDto
                {
                    id_user = c.User.id_user,
                    nom_user = c.User.nom_user,
                    prenom_user = c.User.prenom_user,
                    email_user = c.User.email_user,
                    role_user = c.User.role_user
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetCommandsCommentairesCountByUserId(int userId)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.CommandsCommentaires
            .Where(c => c.id_user == userId)
            .CountAsync();
    }

    public async Task<ReadExtendedCommandCommentaireDto> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null)
    {
        return await _context.CommandsCommentaires
            .Where(c => c.id_command_commentaire == id && (CommandId == null || c.id_command == CommandId) && (userId == null || c.id_user == userId))
            .Select(c => new ReadExtendedCommandCommentaireDto
            {
                id_command_commentaire = c.id_command_commentaire,
                id_command = c.id_command,
                id_user = c.id_user,
                contenu_command_commentaire = c.contenu_command_commentaire,
                date_command_commentaire = c.date_command_commentaire,
                date_modif_command_commentaire = c.date_modif_command_commentaire,
                command = expand != null && expand.Contains("command") ? new ReadCommandDto
                {
                    id_command = c.Command.id_command,
                    prix_command = c.Command.prix_command,
                    url_command = c.Command.url_command,
                    status_command = c.Command.status_command,
                    date_command = c.Command.date_command,
                    date_livraison_command = c.Command.date_livraison_command
                } : null,
                user = expand != null && expand.Contains("user") ? new ReadUserDto
                {
                    id_user = c.User.id_user,
                    nom_user = c.User.nom_user,
                    prenom_user = c.User.prenom_user,
                    email_user = c.User.email_user,
                    role_user = c.User.role_user
                } : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Commentaire with id {id} not found");
    }

    public async Task<ReadCommandCommentaireDto> CreateCommentaire(CreateCommandCommentaireDto commentaireDto)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commentaireDto.id_command))
        {
            throw new KeyNotFoundException($"Command with id {commentaireDto.id_command} not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == commentaireDto.id_user))
        {
            throw new KeyNotFoundException($"User with id {commentaireDto.id_user} not found");
        }
        var newCommentaire = new CommandsCommentaires
        {
            id_command = commentaireDto.id_command,
            id_user = commentaireDto.id_user,
            contenu_command_commentaire = commentaireDto.contenu_command_commentaire,
            date_command_commentaire = DateTime.Now,
            date_modif_command_commentaire = DateTime.Now
        };
        _context.CommandsCommentaires.Add(newCommentaire);
        await _context.SaveChangesAsync();
        return new ReadCommandCommentaireDto
        {
            id_command_commentaire = newCommentaire.id_command_commentaire,
            id_command = newCommentaire.id_command,
            id_user = newCommentaire.id_user,
            contenu_command_commentaire = newCommentaire.contenu_command_commentaire,
            date_command_commentaire = newCommentaire.date_command_commentaire,
            date_modif_command_commentaire = newCommentaire.date_modif_command_commentaire
        };
    }

    public async Task<ReadCommandCommentaireDto> UpdateCommentaire(int id, UpdateCommandCommentaireDto commentaireDto, int? userId = null, int? CommandId = null)
    {
        var commentaireToUpdate = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToUpdate is null) || (CommandId is not null && commentaireToUpdate.id_command != CommandId) || (userId is not null && commentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }
        if (commentaireDto.contenu_command_commentaire is not null)
        {
            commentaireToUpdate.contenu_command_commentaire = commentaireDto.contenu_command_commentaire;
        }
        commentaireToUpdate.date_modif_command_commentaire = DateTime.Now;
        await _context.SaveChangesAsync();
        return new ReadCommandCommentaireDto
        {
            id_command_commentaire = commentaireToUpdate.id_command_commentaire,
            id_command = commentaireToUpdate.id_command,
            id_user = commentaireToUpdate.id_user,
            contenu_command_commentaire = commentaireToUpdate.contenu_command_commentaire,
            date_command_commentaire = commentaireToUpdate.date_command_commentaire,
            date_modif_command_commentaire = commentaireToUpdate.date_modif_command_commentaire
        };
    }

    public async Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null)
    {
        var commentaireToDelete = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToDelete is null) || (CommandId is not null && commentaireToDelete.id_command != CommandId) || (userId is not null && commentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }
        _context.CommandsCommentaires.Remove(commentaireToDelete);
        await _context.SaveChangesAsync();
    }
}