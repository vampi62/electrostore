using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetService;

public class ProjetService : IProjetService
{
    private readonly ApplicationDbContext _context;

    public ProjetService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedProjetDto>> GetProjets(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Projets.AsQueryable();
        if (idResearch != null && idResearch.Any())
        {
            query = query.Where(b => idResearch.Contains(b.id_projet));
        }
        return await query
            .Skip(offset)
            .Take(limit)
            .Select(p => new ReadExtendedProjetDto
            {
                id_projet = p.id_projet,
                nom_projet = p.nom_projet,
                description_projet = p.description_projet,
                url_projet = p.url_projet,
                status_projet = p.status_projet,
                date_debut_projet = p.date_debut_projet,
                date_fin_projet = p.date_fin_projet,
                projets_commentaires = expand != null && expand.Contains("projets_commentaires") ? p.ProjetsCommentaires.Select(pc => new ReadProjetCommentaireDto
                {
                    id_projet_commentaire = pc.id_projet_commentaire,
                    id_projet = pc.id_projet,
                    id_user = pc.id_user,
                    contenu_projet_commentaire = pc.contenu_projet_commentaire,
                    date_projet_commentaire = pc.date_projet_commentaire,
                    date_modif_projet_commentaire = pc.date_modif_projet_commentaire
                }) : null,
                projets_documents = expand != null && expand.Contains("projets_documents") ? p.ProjetsDocuments.Select(pd => new ReadProjetDocumentDto
                {
                    id_projet_document = pd.id_projet_document,
                    id_projet = pd.id_projet,
                    url_projet_document = pd.url_projet_document,
                    name_projet_document = pd.name_projet_document,
                    type_projet_document = pd.type_projet_document,
                    size_projet_document = pd.size_projet_document,
                    date_projet_document = pd.date_projet_document
                }) : null,
                projets_items = expand != null && expand.Contains("projets_items") ? p.ProjetsItems.Select(pi => new ReadProjetItemDto
                {
                    id_projet = pi.id_projet,
                    id_item = pi.id_item,
                    qte_projet_item = pi.qte_projet_item
                }) : null,
                projets_commentaires_count = p.ProjetsCommentaires.Count,
                projets_documents_count = p.ProjetsDocuments.Count,
                projets_items_count = p.ProjetsItems.Count
            })
            .ToListAsync();
    }

    public async Task<int> GetProjetsCount()
    {
        return await _context.Projets.CountAsync();
    }

    public async Task<ReadExtendedProjetDto> GetProjetById(int id, List<string>? expand = null)
    {
        var projet = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        return new ReadExtendedProjetDto
        {
            id_projet = projet.id_projet,
            nom_projet = projet.nom_projet,
            description_projet = projet.description_projet,
            url_projet = projet.url_projet,
            status_projet = projet.status_projet,
            date_debut_projet = projet.date_debut_projet,
            date_fin_projet = projet.date_fin_projet,
            projets_commentaires = expand != null && expand.Contains("projets_commentaires") ? projet.ProjetsCommentaires.Select(pc => new ReadProjetCommentaireDto
            {
                id_projet_commentaire = pc.id_projet_commentaire,
                id_projet = pc.id_projet,
                id_user = pc.id_user,
                contenu_projet_commentaire = pc.contenu_projet_commentaire,
                date_projet_commentaire = pc.date_projet_commentaire,
                date_modif_projet_commentaire = pc.date_modif_projet_commentaire
            }) : null,
            projets_documents = expand != null && expand.Contains("projets_documents") ? projet.ProjetsDocuments.Select(pd => new ReadProjetDocumentDto
            {
                id_projet_document = pd.id_projet_document,
                id_projet = pd.id_projet,
                url_projet_document = pd.url_projet_document,
                name_projet_document = pd.name_projet_document,
                type_projet_document = pd.type_projet_document,
                size_projet_document = pd.size_projet_document,
                date_projet_document = pd.date_projet_document
            }) : null,
            projets_items = expand != null && expand.Contains("projets_items") ? projet.ProjetsItems.Select(pi => new ReadProjetItemDto
            {
                id_projet = pi.id_projet,
                id_item = pi.id_item,
                qte_projet_item = pi.qte_projet_item
            }) : null,
            projets_commentaires_count = projet.ProjetsCommentaires.Count,
            projets_documents_count = projet.ProjetsDocuments.Count,
            projets_items_count = projet.ProjetsItems.Count
        };
    }

    public async Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto)
    {
        var newProjet = new Projets
        {
            nom_projet = projetDto.nom_projet,
            description_projet = projetDto.description_projet,
            url_projet = projetDto.url_projet,
            status_projet = projetDto.status_projet,
            date_debut_projet = projetDto.date_debut_projet,
            date_fin_projet = projetDto.date_fin_projet
        };
        _context.Projets.Add(newProjet);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString()));
        }
        return new ReadProjetDto
        {
            id_projet = newProjet.id_projet,
            nom_projet = newProjet.nom_projet,
            description_projet = newProjet.description_projet,
            url_projet = newProjet.url_projet,
            status_projet = newProjet.status_projet,
            date_debut_projet = newProjet.date_debut_projet,
            date_fin_projet = newProjet.date_fin_projet
        };
    }

    public async Task<ReadProjetDto> UpdateProjet(int id, UpdateProjetDto projetDto)
    {
        var projetToUpdate = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        if (projetDto.nom_projet is not null)
        {
            projetToUpdate.nom_projet = projetDto.nom_projet;
        }
        if (projetDto.description_projet is not null)
        {
            projetToUpdate.description_projet = projetDto.description_projet;
        }
        if (projetDto.url_projet is not null)
        {
            projetToUpdate.url_projet = projetDto.url_projet;
        }
        if (projetDto.status_projet is not null)
        {
            projetToUpdate.status_projet = projetDto.status_projet;
        }
        if (projetDto.date_debut_projet is not null)
        {
            projetToUpdate.date_debut_projet = projetDto.date_debut_projet.Value;
        }
        if (projetDto.date_fin_projet is not null)
        {
            projetToUpdate.date_fin_projet = projetDto.date_fin_projet;
        }
        await _context.SaveChangesAsync();
        return new ReadProjetDto
        {
            id_projet = projetToUpdate.id_projet,
            nom_projet = projetToUpdate.nom_projet,
            description_projet = projetToUpdate.description_projet,
            url_projet = projetToUpdate.url_projet,
            status_projet = projetToUpdate.status_projet,
            date_debut_projet = projetToUpdate.date_debut_projet,
            date_fin_projet = projetToUpdate.date_fin_projet
        };
    }

    public async Task DeleteProjet(int id)
    {
        var projetToDelete = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        _context.Projets.Remove(projetToDelete);
        await _context.SaveChangesAsync();
        //remove folder in wwwroot/projetDocuments
        if (Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", id.ToString())))
        {
            Directory.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", id.ToString()), true);
        }
    }
}