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

    public async Task<IEnumerable<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }

        return await _context.ProjetsCommentaires
            .Where(p => p.id_projet == projetId)
            .OrderByDescending(p => p.date_projet_commentaire)
            .Skip(offset)
            .Take(limit)
            .Select(p => new ReadExtendedProjetCommentaireDto
            {
                id_projet_commentaire = p.id_projet_commentaire,
                id_projet = p.id_projet,
                id_user = p.id_user,
                contenu_projet_commentaire = p.contenu_projet_commentaire,
                date_projet_commentaire = p.date_projet_commentaire,
                date_modif_projet_commentaire = p.date_modif_projet_commentaire,
                projet = expand != null && expand.Contains("projet") ? new ReadProjetDto
                {
                    id_projet = p.Projet.id_projet,
                    nom_projet = p.Projet.nom_projet,
                    description_projet = p.Projet.description_projet,
                    url_projet = p.Projet.url_projet,
                    status_projet = p.Projet.status_projet,
                    date_debut_projet = p.Projet.date_debut_projet,
                    date_fin_projet = p.Projet.date_fin_projet
                } : null,
                user = expand != null && expand.Contains("user") ? new ReadUserDto
                {
                    id_user = p.User.id_user,
                    nom_user = p.User.nom_user,
                    prenom_user = p.User.prenom_user,
                    email_user = p.User.email_user,
                    role_user = p.User.role_user
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetProjetCommentairesCountByProjetId(int projetId)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id {projetId} not found");
        }
        return await _context.ProjetsCommentaires
            .CountAsync(p => p.id_projet == projetId);
    }

    public async Task<IEnumerable<ReadExtendedProjetCommentaireDto>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.ProjetsCommentaires
            .Where(p => p.id_user == userId)
            .OrderByDescending(p => p.date_projet_commentaire)
            .Skip(offset)
            .Take(limit)
            .Select(p => new ReadExtendedProjetCommentaireDto
            {
                id_projet_commentaire = p.id_projet_commentaire,
                id_projet = p.id_projet,
                id_user = p.id_user,
                contenu_projet_commentaire = p.contenu_projet_commentaire,
                date_projet_commentaire = p.date_projet_commentaire,
                date_modif_projet_commentaire = p.date_modif_projet_commentaire,
                projet = expand != null && expand.Contains("projet") ? new ReadProjetDto
                {
                    id_projet = p.Projet.id_projet,
                    nom_projet = p.Projet.nom_projet,
                    description_projet = p.Projet.description_projet,
                    url_projet = p.Projet.url_projet,
                    status_projet = p.Projet.status_projet,
                    date_debut_projet = p.Projet.date_debut_projet,
                    date_fin_projet = p.Projet.date_fin_projet
                } : null,
                user = expand != null && expand.Contains("user") ? new ReadUserDto
                {
                    id_user = p.User.id_user,
                    nom_user = p.User.nom_user,
                    prenom_user = p.User.prenom_user,
                    email_user = p.User.email_user,
                    role_user = p.User.role_user
                } : null
            })
            .ToListAsync();
    }

    public async Task<int> GetProjetCommentairesCountByUserId(int userId)
    {
        // check if the user exists
        if (!await _context.Users.AnyAsync(u => u.id_user == userId))
        {
            throw new KeyNotFoundException($"User with id {userId} not found");
        }
        return await _context.ProjetsCommentaires
            .CountAsync(p => p.id_user == userId);
    }

    public async Task<ReadExtendedProjetCommentaireDto> GetProjetCommentairesById(int id, int? userId = null, int? projetId = null, List<string>? expand = null)
    {
        return await _context.ProjetsCommentaires
            .Where(p => p.id_projet_commentaire == id && (projetId == null || p.id_projet == projetId) && (userId == null || p.id_user == userId))
            .Select(p => new ReadExtendedProjetCommentaireDto
            {
                id_projet_commentaire = p.id_projet_commentaire,
                id_projet = p.id_projet,
                id_user = p.id_user,
                contenu_projet_commentaire = p.contenu_projet_commentaire,
                date_projet_commentaire = p.date_projet_commentaire,
                date_modif_projet_commentaire = p.date_modif_projet_commentaire,
                projet = expand != null && expand.Contains("projet") ? new ReadProjetDto
                {
                    id_projet = p.Projet.id_projet,
                    nom_projet = p.Projet.nom_projet,
                    description_projet = p.Projet.description_projet,
                    url_projet = p.Projet.url_projet,
                    status_projet = p.Projet.status_projet,
                    date_debut_projet = p.Projet.date_debut_projet,
                    date_fin_projet = p.Projet.date_fin_projet
                } : null,
                user = expand != null && expand.Contains("user") ? new ReadUserDto
                {
                    id_user = p.User.id_user,
                    nom_user = p.User.nom_user,
                    prenom_user = p.User.prenom_user,
                    email_user = p.User.email_user,
                    role_user = p.User.role_user
                } : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjetCommentaire with id {id} not found");
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
            date_modif_projet_commentaire = newProjetCommentaire.date_modif_projet_commentaire
        };
    }

    public async Task<ReadProjetCommentaireDto> UpdateProjetCommentaire(int id, UpdateProjetCommentaireDto projetCommentaireDto, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToUpdate = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToUpdate is null) || (projetId is not null && projetCommentaireToUpdate.id_projet != projetId) || (userId is not null && projetCommentaireToUpdate.id_user != userId))
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
            date_modif_projet_commentaire = projetCommentaireToUpdate.date_modif_projet_commentaire
        };
    }

    public async Task DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null)
    {
        var projetCommentaireToDelete = await _context.ProjetsCommentaires.FindAsync(id);
        if ((projetCommentaireToDelete is null) || (projetId is not null && projetCommentaireToDelete.id_projet != projetId) || (userId is not null && projetCommentaireToDelete.id_user != userId))
        {
            throw new KeyNotFoundException($"ProjetCommentaire with id {id} not found");
        }
        _context.ProjetsCommentaires.Remove(projetCommentaireToDelete);
        await _context.SaveChangesAsync();
    }
}