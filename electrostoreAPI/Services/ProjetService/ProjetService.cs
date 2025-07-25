using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ProjetService;

public class ProjetService : IProjetService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjetService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedProjetDto>> GetProjets(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Projets.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(p => idResearch.Contains(p.id_projet));
        }
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(p => p.id_projet);
        var projet = await query
            .Select(p => new
            {
                Projet = p,
                ProjetsCommentairesCount = p.ProjetsCommentaires.Count,
                ProjetsDocumentsCount = p.ProjetsDocuments.Count,
                ProjetsItemsCount = p.ProjetsItems.Count,
                ProjetsCommentaires = expand != null && expand.Contains("projets_commentaires") ? p.ProjetsCommentaires.Take(20).ToList() : null,
                ProjetsDocuments = expand != null && expand.Contains("projets_documents") ? p.ProjetsDocuments.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("projets_items") ? p.ProjetsItems.Take(20).ToList() : null
            })
            .ToListAsync();
        return projet.Select(p => {
            return _mapper.Map<ReadExtendedProjetDto>(p.Projet) with
            {
                projets_commentaires_count = p.ProjetsCommentairesCount,
                projets_documents_count = p.ProjetsDocumentsCount,
                projets_items_count = p.ProjetsItemsCount,
                projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(p.ProjetsCommentaires),
                projets_documents = _mapper.Map<IEnumerable<ReadProjetDocumentDto>>(p.ProjetsDocuments),
                projets_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(p.ProjetsItems)
            };
        }).ToList();
    }

    public async Task<int> GetProjetsCount()
    {
        return await _context.Projets.CountAsync();
    }

    public async Task<ReadExtendedProjetDto> GetProjetById(int id, List<string>? expand = null)
    {
        var query = _context.Projets.AsQueryable();
        query = query.Where(p => p.id_projet == id);
        var projet = await query
            .Select(p => new
            {
                Projet = p,
                ProjetsCommentairesCount = p.ProjetsCommentaires.Count,
                ProjetsDocumentsCount = p.ProjetsDocuments.Count,
                ProjetsItemsCount = p.ProjetsItems.Count,
                ProjetsCommentaires = expand != null && expand.Contains("projets_commentaires") ? p.ProjetsCommentaires.Take(20).ToList() : null,
                ProjetsDocuments = expand != null && expand.Contains("projets_documents") ? p.ProjetsDocuments.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("projets_items") ? p.ProjetsItems.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        return _mapper.Map<ReadExtendedProjetDto>(projet.Projet) with
        {
            projets_commentaires_count = projet.ProjetsCommentairesCount,
            projets_documents_count = projet.ProjetsDocumentsCount,
            projets_items_count = projet.ProjetsItemsCount,
            projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(projet.ProjetsCommentaires),
            projets_documents = _mapper.Map<IEnumerable<ReadProjetDocumentDto>>(projet.ProjetsDocuments),
            projets_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(projet.ProjetsItems)
        };
    }

    public async Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto)
    {
        var newProjet = _mapper.Map<Projets>(projetDto);
        _context.Projets.Add(newProjet);
        await _context.SaveChangesAsync();
        if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString())))
        {
            Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/projetDocuments", newProjet.id_projet.ToString()));
        }
        return _mapper.Map<ReadProjetDto>(newProjet);
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
        return _mapper.Map<ReadProjetDto>(projetToUpdate);
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