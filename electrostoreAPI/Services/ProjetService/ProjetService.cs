using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.FileService;

namespace electrostore.Services.ProjetService;

public class ProjetService : IProjetService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _projetDocumentsPath = "projetDocuments";

    public ProjetService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
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
                ProjetsItems = expand != null && expand.Contains("projets_items") ? p.ProjetsItems.Take(20).ToList() : null,
                ProjetsProjetTags = expand != null && expand.Contains("projets_projet_tags") ? p.ProjetsProjetTags.Take(20).ToList() : null
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
                projets_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(p.ProjetsItems),
                projets_projet_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(p.ProjetsProjetTags)
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
                ProjetsItems = expand != null && expand.Contains("projets_items") ? p.ProjetsItems.Take(20).ToList() : null,
                ProjetsProjetTags = expand != null && expand.Contains("projets_projet_tags") ? p.ProjetsProjetTags.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        return _mapper.Map<ReadExtendedProjetDto>(projet.Projet) with
        {
            projets_commentaires_count = projet.ProjetsCommentairesCount,
            projets_documents_count = projet.ProjetsDocumentsCount,
            projets_items_count = projet.ProjetsItemsCount,
            projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(projet.ProjetsCommentaires),
            projets_documents = _mapper.Map<IEnumerable<ReadProjetDocumentDto>>(projet.ProjetsDocuments),
            projets_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(projet.ProjetsItems),
            projets_projet_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(projet.ProjetsProjetTags)
        };
    }

    public async Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto)
    {
        if (projetDto.status_projet == Enums.ProjetStatus.Completed || projetDto.status_projet == Enums.ProjetStatus.Archived)
        {
            if (!projetDto.date_fin_projet.HasValue)
            {
                throw new ArgumentException("date_fin_projet is required when status_projet is Completed or Archived");
            }
        }
        else
        {
            projetDto = projetDto with { date_fin_projet = null };
        }
        var newProjet = _mapper.Map<Projets>(projetDto);
        _context.Projets.Add(newProjet);
        await _fileService.CreateDirectory(Path.Combine(_projetDocumentsPath, newProjet.id_projet.ToString()));
        await _context.SaveChangesAsync();
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
            projetToUpdate.status_projet = projetDto.status_projet.Value;
        }
        if (projetDto.date_debut_projet is not null)
        {
            projetToUpdate.date_debut_projet = projetDto.date_debut_projet.Value;
        }
        if (projetDto.date_fin_projet is not null)
        {
            projetToUpdate.date_fin_projet = projetDto.date_fin_projet;
        }
        if (projetToUpdate.date_fin_projet.HasValue && projetToUpdate.date_fin_projet < projetToUpdate.date_debut_projet)
        {
            throw new ArgumentException("date_fin_projet cannot be earlier than date_debut_projet");
        }
        if (projetToUpdate.status_projet == Enums.ProjetStatus.Completed || projetToUpdate.status_projet == Enums.ProjetStatus.Archived)
        {
            if (!projetToUpdate.date_fin_projet.HasValue)
            {
                throw new ArgumentException("date_fin_projet is required when status_projet is Completed or Archived");
            }
        }
        else
        {
            projetToUpdate.date_fin_projet = null;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetDto>(projetToUpdate);
    }

    public async Task DeleteProjet(int id)
    {
        var projetToDelete = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id {id} not found");
        _context.Projets.Remove(projetToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_projetDocumentsPath, id.ToString()));
        await _context.SaveChangesAsync();
    }
}