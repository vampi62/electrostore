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

    public async Task<IEnumerable<ReadProjetCommentaireDto>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            
        }

        return await _context.ProjetsCommentaires
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_projet == projetId)
            .Select(p => new ReadProjetCommentaireDto
            {
                id_projet_commentaire = p.id_projet_commentaire,
                id_projet = p.id_projet,
                id_user = p.id_user,
                contenu_projet_commentaire = p.contenu_projet_commentaire,
                date_projet_commentaire = p.date_projet_commentaire,
                date_modif_projet_commentaire = p.date_modif_projet_commentaire,
                user_name = p.User.nom_user + " " + p.User.prenom_user
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadProjetCommentaireDto>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.ProjetsCommentaires
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_user == userId)
            .Select(p => new ReadProjetCommentaireDto
            {
                id_projet_commentaire = p.id_projet_commentaire,
                id_projet = p.id_projet,
                id_user = p.id_user,
                contenu_projet_commentaire = p.contenu_projet_commentaire,
                date_projet_commentaire = p.date_projet_commentaire,
                date_modif_projet_commentaire = p.date_modif_projet_commentaire,
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

    public async Task<ReadProjetCommentaireDto> GetProjetCommentairesByCommentaireId(int id, int? userId = null, int? projetId = null)
    {
        var projetCommentaire = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaire == null) || (projetId != null && projetCommentaire.id_projet != projetId) || (userId != null && projetCommentaire.id_user != userId))
        {
            throw new KeyNotFoundException($"ProjetCommentaire with id {id} not found");
        }
        return new ReadProjetCommentaireDto
        {
            id_projet_commentaire = projetCommentaire.id_projet_commentaire,
            id_projet = projetCommentaire.id_projet,
            id_user = projetCommentaire.id_user,
            contenu_projet_commentaire = projetCommentaire.contenu_projet_commentaire,
            date_projet_commentaire = projetCommentaire.date_projet_commentaire,
            date_modif_projet_commentaire = projetCommentaire.date_modif_projet_commentaire,
            user_name = projetCommentaire.User.nom_user + " " + projetCommentaire.User.prenom_user
        };
    }

    public async Task<ReadProjetCommentaireDto> CreateProjetCommentaire(CreateProjetCommentaireDto projetCommentaireDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetCommentaireDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id {projetCommentaireDto.id_projet} not found");
        }
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == projetCommentaireDto.id_user))
        {
            throw new KeyNotFoundException($"User with id {projetCommentaireDto.id_user} not found");
        }
        var newProjetCommentaire = new ProjetsCommentaires
        {
            id_projet = projetCommentaireDto.id_projet,
            id_user = projetCommentaireDto.id_user,
            contenu_projet_commentaire = projetCommentaireDto.contenu_projet_commentaire,
            date_projet_commentaire = DateTime.Now,
            date_modif_projet_commentaire = DateTime.Now
        };
        _context.ProjetsCommentaires.Add(newProjetCommentaire);
        await _context.SaveChangesAsync();
        return new ReadProjetCommentaireDto
        {
            id_projet_commentaire = newProjetCommentaire.id_projet_commentaire,
            id_projet = newProjetCommentaire.id_projet,
            id_user = newProjetCommentaire.id_user,
            contenu_projet_commentaire = newProjetCommentaire.contenu_projet_commentaire,
            date_projet_commentaire = newProjetCommentaire.date_projet_commentaire,
            date_modif_projet_commentaire = newProjetCommentaire.date_modif_projet_commentaire,
            user_name = newProjetCommentaire.User.nom_user + " " + newProjetCommentaire.User.prenom_user
        };
    }

    public async Task<ReadProjetCommentaireDto> UpdateProjetCommentaire(int id, UpdateProjetCommentaireDto projetCommentaireDto, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToUpdate = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToUpdate == null) || (projetId != null && projetCommentaireToUpdate.id_projet != projetId) || (userId != null && projetCommentaireToUpdate.id_user != userId))
        {
            throw new KeyNotFoundException($"ProjetCommentaire with id {id} not found");
        }
        projetCommentaireToUpdate.contenu_projet_commentaire = projetCommentaireDto.contenu_projet_commentaire ?? projetCommentaireToUpdate.contenu_projet_commentaire;
        projetCommentaireToUpdate.date_modif_projet_commentaire = DateTime.Now;
        await _context.SaveChangesAsync();
        return new ReadProjetCommentaireDto
        {
            id_projet_commentaire = projetCommentaireToUpdate.id_projet_commentaire,
            id_projet = projetCommentaireToUpdate.id_projet,
            id_user = projetCommentaireToUpdate.id_user,
            contenu_projet_commentaire = projetCommentaireToUpdate.contenu_projet_commentaire,
            date_modif_projet_commentaire = projetCommentaireToUpdate.date_modif_projet_commentaire,
            user_name = projetCommentaireToUpdate.User.nom_user + " " + projetCommentaireToUpdate.User.prenom_user
        };
    }

    public async Task DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToDelete = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToDelete == null) || (projetId != null && projetCommentaireToDelete.id_projet != projetId) || (userId != null && projetCommentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"ProjetCommentaire with id {id} not found");
        }
        _context.ProjetsCommentaires.Remove(projetCommentaireToDelete);
        await _context.SaveChangesAsync();
    }
}