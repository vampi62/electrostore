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

    public async Task<IEnumerable<ReadCommandCommentaireDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0)
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
            .Select(c => new ReadCommandCommentaireDto
            {
                id_commandcommentaire = c.id_commandcommentaire,
                id_command = c.id_command,
                id_user = c.id_user,
                contenu_command_commentaire = c.contenu_command_commentaire,
                date_command_commentaire = c.date_command_commentaire,
                date_modif_command_commentaire = c.date_modif_command_commentaire,
                command = new ReadCommandDto
                {
                    id_command = c.Command.id_command,
                    prix_command = c.Command.prix_command,
                    url_command = c.Command.url_command,
                    status_command = c.Command.status_command,
                    date_command = c.Command.date_command,
                    date_livraison_command = c.Command.date_livraison_command
                },
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

    public async Task<IEnumerable<ReadCommandCommentaireDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0)
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
            .Select(c => new ReadCommandCommentaireDto
            {
                id_commandcommentaire = c.id_commandcommentaire,
                id_command = c.id_command,
                id_user = c.id_user,
                contenu_command_commentaire = c.contenu_command_commentaire,
                date_command_commentaire = c.date_command_commentaire,
                date_modif_command_commentaire = c.date_modif_command_commentaire,
                user_name = c.User.nom_user + " " + c.User.prenom_user
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

    public async Task<ReadCommandCommentaireDto> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null)
    {
        var commentaire = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaire == null) || (CommandId != null && commentaire.id_command != CommandId) || (userId != null && commentaire.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }

        return new ReadCommandCommentaireDto
        {
            id_commandcommentaire = commentaire.id_commandcommentaire,
            id_command = commentaire.id_command,
            id_user = commentaire.id_user,
            contenu_command_commentaire = commentaire.contenu_command_commentaire,
            date_command_commentaire = commentaire.date_command_commentaire,
            date_modif_command_commentaire = commentaire.date_modif_command_commentaire,
            user_name = commentaire.User.nom_user + " " + commentaire.User.prenom_user
        };
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
            id_commandcommentaire = newCommentaire.id_commandcommentaire,
            id_command = newCommentaire.id_command,
            id_user = newCommentaire.id_user,
            contenu_command_commentaire = newCommentaire.contenu_command_commentaire,
            date_command_commentaire = newCommentaire.date_command_commentaire,
            date_modif_command_commentaire = newCommentaire.date_modif_command_commentaire,
            user_name = newCommentaire.User.nom_user + " " + newCommentaire.User.prenom_user
        };
    }

    public async Task<ReadCommandCommentaireDto> UpdateCommentaire(int id, UpdateCommandCommentaireDto commentaireDto, int? userId = null, int? CommandId = null)
    {
        var commentaireToUpdate = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToUpdate == null) || (CommandId != null && commentaireToUpdate.id_command != CommandId) || (userId != null && commentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }
        if (commentaireDto.contenu_command_commentaire != null)
        {
            commentaireToUpdate.contenu_command_commentaire = commentaireDto.contenu_command_commentaire;
        }
        commentaireToUpdate.date_modif_command_commentaire = DateTime.Now;
        await _context.SaveChangesAsync();
        return new ReadCommandCommentaireDto
        {
            id_commandcommentaire = commentaireToUpdate.id_commandcommentaire,
            id_command = commentaireToUpdate.id_command,
            id_user = commentaireToUpdate.id_user,
            contenu_command_commentaire = commentaireToUpdate.contenu_command_commentaire,
            date_command_commentaire = commentaireToUpdate.date_command_commentaire,
            date_modif_command_commentaire = commentaireToUpdate.date_modif_command_commentaire,
            user_name = commentaireToUpdate.User.nom_user + " " + commentaireToUpdate.User.prenom_user
        };
    }

    public async Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null)
    {
        var commentaireToDelete = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToDelete == null) || (CommandId != null && commentaireToDelete.id_command != CommandId) || (userId != null && commentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"Commentaire with id {id} not found");
        }
        _context.CommandsCommentaires.Remove(commentaireToDelete);
        await _context.SaveChangesAsync();
    }
}