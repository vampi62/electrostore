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

    public async Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(p => p.id_command == CommandId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { CommandId = new string[] { "Command not found" } } });
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
                contenu_commandcommentaire = c.contenu_commandcommentaire,
                date_commandcommentaire = c.date_commandcommentaire,
                date_modif_commandcommentaire = c.date_modif_commandcommentaire,
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

    public async Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { userId = new string[] { "User not found" } } });
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
                contenu_commandcommentaire = c.contenu_commandcommentaire,
                date_commandcommentaire = c.date_commandcommentaire,
                date_modif_commandcommentaire = c.date_modif_commandcommentaire,
                user_name = c.User.nom_user + " " + c.User.prenom_user
            })
            .ToListAsync();
    }

    public async Task<ActionResult<ReadCommandCommentaireDto>> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null)
    {
        var commentaire = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaire == null) || (CommandId != null && commentaire.id_command != CommandId) || (userId != null && commentaire.id_user != userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id = new string[] { "CommandCommentaire not found" } } });
        }

        return new ReadCommandCommentaireDto
        {
            id_commandcommentaire = commentaire.id_commandcommentaire,
            id_command = commentaire.id_command,
            id_user = commentaire.id_user,
            contenu_commandcommentaire = commentaire.contenu_commandcommentaire,
            date_commandcommentaire = commentaire.date_commandcommentaire,
            date_modif_commandcommentaire = commentaire.date_modif_commandcommentaire,
            user_name = commentaire.User.nom_user + " " + commentaire.User.prenom_user
        };
    }

    public async Task<ActionResult<ReadCommandCommentaireDto>> CreateCommentaire(CreateCommandCommentaireDto commentaireDto)
    {
        // check if the command exists
        if (!await _context.Commands.AnyAsync(c => c.id_command == commentaireDto.id_command))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { CommandId = new string[] { "Command not found" } } });
        }

        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == commentaireDto.id_user))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { UserId = new string[] { "User not found" } } });
        }

        var newCommentaire = new CommandsCommentaires
        {
            id_command = commentaireDto.id_command,
            id_user = commentaireDto.id_user,
            contenu_commandcommentaire = commentaireDto.contenu_commandcommentaire,
            date_commandcommentaire = DateTime.Now,
            date_modif_commandcommentaire = DateTime.Now
        };

        _context.CommandsCommentaires.Add(newCommentaire);
        await _context.SaveChangesAsync();

        return new ReadCommandCommentaireDto
        {
            id_commandcommentaire = newCommentaire.id_commandcommentaire,
            id_command = newCommentaire.id_command,
            id_user = newCommentaire.id_user,
            contenu_commandcommentaire = newCommentaire.contenu_commandcommentaire,
            date_commandcommentaire = newCommentaire.date_commandcommentaire,
            date_modif_commandcommentaire = newCommentaire.date_modif_commandcommentaire,
            user_name = newCommentaire.User.nom_user + " " + newCommentaire.User.prenom_user
        };
    }

    public async Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommentaire(int id, UpdateCommandCommentaireDto commentaireDto, int? userId = null, int? CommandId = null)
    {
        var commentaireToUpdate = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToUpdate == null) || (CommandId != null && commentaireToUpdate.id_command != CommandId) || (userId != null && commentaireToUpdate.id_user != userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id = new string[] { "CommandCommentaire not found" } } });
        }

        if (commentaireDto.contenu_commandcommentaire != null)
        {
            commentaireToUpdate.contenu_commandcommentaire = commentaireDto.contenu_commandcommentaire;
        }
        commentaireToUpdate.date_modif_commandcommentaire = DateTime.Now;

        await _context.SaveChangesAsync();

        return new ReadCommandCommentaireDto
        {
            id_commandcommentaire = commentaireToUpdate.id_commandcommentaire,
            id_command = commentaireToUpdate.id_command,
            id_user = commentaireToUpdate.id_user,
            contenu_commandcommentaire = commentaireToUpdate.contenu_commandcommentaire,
            date_commandcommentaire = commentaireToUpdate.date_commandcommentaire,
            date_modif_commandcommentaire = commentaireToUpdate.date_modif_commandcommentaire,
            user_name = commentaireToUpdate.User.nom_user + " " + commentaireToUpdate.User.prenom_user
        };
    }

    public async Task<IActionResult> DeleteCommentaire(int id, int? userId = null, int? CommandId = null)
    {
        var commentaireToDelete = await _context.CommandsCommentaires.FindAsync(id);
        if ((commentaireToDelete == null) || (CommandId != null && commentaireToDelete.id_command != CommandId) || (userId != null && commentaireToDelete.id_user != userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id = new string[] { "CommandCommentaire not found" } } });
        }

        _context.CommandsCommentaires.Remove(commentaireToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}