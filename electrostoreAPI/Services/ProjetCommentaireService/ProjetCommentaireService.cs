using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetCommentaireService;

public class ProjetCommentaireService : IProjetCommentaireService
{
    private readonly ApplicationDbContext _context;

    public ProjetCommentaireService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ActionResult<IEnumerable<ReadProjetCommentaireDto>>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "Projet not found" } }});
        }

        return await _context.ProjetsCommentaires
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_projet == projetId)
            .Select(p => new ReadProjetCommentaireDto
            {
                id_projetcommentaire = p.id_projetcommentaire,
                id_projet = p.id_projet,
                id_user = p.id_user,
                contenu_projetcommentaire = p.contenu_projetcommentaire,
                date_projetcommentaire = p.date_projetcommentaire,
                date_modif_projetcommentaire = p.date_modif_projetcommentaire,
                user_name = p.User.nom_user + " " + p.User.prenom_user
            })
            .ToListAsync();
    }

    public async Task<ActionResult<IEnumerable<ReadProjetCommentaireDto>>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_user = new string[] { "User not found" } }});
        }

        return await _context.ProjetsCommentaires
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_user == userId)
            .Select(p => new ReadProjetCommentaireDto
            {
                id_projetcommentaire = p.id_projetcommentaire,
                id_projet = p.id_projet,
                id_user = p.id_user,
                contenu_projetcommentaire = p.contenu_projetcommentaire,
                date_projetcommentaire = p.date_projetcommentaire,
                date_modif_projetcommentaire = p.date_modif_projetcommentaire,
                user_name = p.User.nom_user + " " + p.User.prenom_user,
                projet = new ReadProjetDto
                {
                    id_projet = p.Projet.id_projet,
                    nom_projet = p.Projet.nom_projet,
                    description_projet = p.Projet.description_projet,
                    url_projet = p.Projet.url_projet,
                    status_projet = p.Projet.status_projet,
                    date_projet = p.Projet.date_projet,
                    date_fin_projet = p.Projet.date_fin_projet
                }
            })
            .ToListAsync();
    }

    public async Task<ActionResult<ReadProjetCommentaireDto>> GetProjetCommentairesByCommentaireId(int id, int? userId = null, int? projetId = null)
    {
        var projetCommentaire = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaire == null) || (projetId != null && projetCommentaire.id_projet != projetId) || (userId != null && projetCommentaire.id_user != userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projetcommentaire = new string[] { "ProjetCommentaire not found" } }});
        }

        return new ReadProjetCommentaireDto
        {
            id_projetcommentaire = projetCommentaire.id_projetcommentaire,
            id_projet = projetCommentaire.id_projet,
            id_user = projetCommentaire.id_user,
            contenu_projetcommentaire = projetCommentaire.contenu_projetcommentaire,
            date_projetcommentaire = projetCommentaire.date_projetcommentaire,
            date_modif_projetcommentaire = projetCommentaire.date_modif_projetcommentaire,
            user_name = projetCommentaire.User.nom_user + " " + projetCommentaire.User.prenom_user
        };
    }

    public async Task<ActionResult<ReadProjetCommentaireDto>> CreateProjetCommentaire(CreateProjetCommentaireDto projetCommentaireDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetCommentaireDto.id_projet))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "Projet not found" } }});
        }

        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == projetCommentaireDto.id_user))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_user = new string[] { "User not found" } }});
        }

        var newProjetCommentaire = new ProjetsCommentaires
        {
            id_projet = projetCommentaireDto.id_projet,
            id_user = projetCommentaireDto.id_user,
            contenu_projetcommentaire = projetCommentaireDto.contenu_projetcommentaire,
            date_projetcommentaire = DateTime.Now,
            date_modif_projetcommentaire = DateTime.Now
        };

        _context.ProjetsCommentaires.Add(newProjetCommentaire);
        await _context.SaveChangesAsync();

        return new ReadProjetCommentaireDto
        {
            id_projetcommentaire = newProjetCommentaire.id_projetcommentaire,
            id_projet = newProjetCommentaire.id_projet,
            id_user = newProjetCommentaire.id_user,
            contenu_projetcommentaire = newProjetCommentaire.contenu_projetcommentaire,
            date_projetcommentaire = newProjetCommentaire.date_projetcommentaire,
            date_modif_projetcommentaire = newProjetCommentaire.date_modif_projetcommentaire,
            user_name = newProjetCommentaire.User.nom_user + " " + newProjetCommentaire.User.prenom_user
        };
    }

    public async Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire(int id, UpdateProjetCommentaireDto projetCommentaireDto, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToUpdate = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToUpdate == null) || (projetId != null && projetCommentaireToUpdate.id_projet != projetId) || (userId != null && projetCommentaireToUpdate.id_user != userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projetcommentaire = new string[] { "ProjetCommentaire not found" } }});
        }

        projetCommentaireToUpdate.contenu_projetcommentaire = projetCommentaireDto.contenu_projetcommentaire ?? projetCommentaireToUpdate.contenu_projetcommentaire;
        projetCommentaireToUpdate.date_modif_projetcommentaire = DateTime.Now;

        await _context.SaveChangesAsync();

        return new ReadProjetCommentaireDto
        {
            id_projetcommentaire = projetCommentaireToUpdate.id_projetcommentaire,
            id_projet = projetCommentaireToUpdate.id_projet,
            id_user = projetCommentaireToUpdate.id_user,
            contenu_projetcommentaire = projetCommentaireToUpdate.contenu_projetcommentaire,
            date_modif_projetcommentaire = projetCommentaireToUpdate.date_modif_projetcommentaire,
            user_name = projetCommentaireToUpdate.User.nom_user + " " + projetCommentaireToUpdate.User.prenom_user
        };
    }

    public async Task<IActionResult> DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToDelete = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToDelete == null) || (projetId != null && projetCommentaireToDelete.id_projet != projetId) || (userId != null && projetCommentaireToDelete.id_user != userId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projetcommentaire = new string[] { "ProjetCommentaire not found" } }});
        }
        _context.ProjetsCommentaires.Remove(projetCommentaireToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}