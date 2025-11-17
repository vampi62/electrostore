using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Models;
using electrostore.Services.FileService;
using electrostore.Services.ProjetStatusService;

namespace electrostore.Services.ProjetService;

public class ProjetService : IProjetService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly IProjetStatusService _projetStatusService;
    private readonly string _projetDocumentsPath = "projetDocuments";

    public ProjetService(IMapper mapper, ApplicationDbContext context, IFileService fileService, IProjetStatusService projetStatusService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
        _projetStatusService = projetStatusService;
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
                ProjetsProjetTagsCount = p.ProjetsProjetTags.Count,
                ProjetsStatusHistoryCount = p.ProjetsStatus.Count,
                DateDebutProjet = p.ProjetsStatus
                    .Where(ps => ps.status_projet == ProjetStatus.InProgress)
                    .OrderBy(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                DateFinProjet = p.ProjetsStatus
                    .Where(ps => ps.status_projet == ProjetStatus.Completed)
                    .OrderByDescending(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                ProjetsCommentaires = expand != null && expand.Contains("projets_commentaires") ? p.ProjetsCommentaires.Take(20).ToList() : null,
                ProjetsDocuments = expand != null && expand.Contains("projets_documents") ? p.ProjetsDocuments.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("projets_items") ? p.ProjetsItems.Take(20).ToList() : null,
                ProjetsProjetTags = expand != null && expand.Contains("projets_projet_tags") ? p.ProjetsProjetTags.Take(20).ToList() : null,
                ProjetsStatus = expand != null && expand.Contains("projets_status_history") ? p.ProjetsStatus.Take(20).ToList() : null
            })
            .ToListAsync();
        return projet.Select(p => {
            return _mapper.Map<ReadExtendedProjetDto>(p.Projet) with
            {
                date_debut_projet = p.DateDebutProjet,
                date_fin_projet = p.DateFinProjet,
                projets_commentaires_count = p.ProjetsCommentairesCount,
                projets_documents_count = p.ProjetsDocumentsCount,
                projets_items_count = p.ProjetsItemsCount,
                projets_tags_count = p.ProjetsProjetTagsCount,
                projets_status_history_count = p.ProjetsStatusHistoryCount,
                projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(p.ProjetsCommentaires),
                projets_documents = _mapper.Map<IEnumerable<ReadProjetDocumentDto>>(p.ProjetsDocuments),
                projets_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(p.ProjetsItems),
                projets_projet_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(p.ProjetsProjetTags),
                projets_status_history = _mapper.Map<IEnumerable<ReadProjetStatusDto>>(p.ProjetsStatus)
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
                ProjetsProjetsTagsCount = p.ProjetsProjetTags.Count,
                ProjetsStatusHistoryCount = p.ProjetsStatus.Count,
                DateDebutProjet = p.ProjetsStatus
                    .Where(ps => ps.status_projet == ProjetStatus.InProgress)
                    .OrderBy(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                DateFinProjet = p.ProjetsStatus
                    .Where(ps => ps.status_projet == ProjetStatus.Completed)
                    .OrderByDescending(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                ProjetsCommentaires = expand != null && expand.Contains("projets_commentaires") ? p.ProjetsCommentaires.Take(20).ToList() : null,
                ProjetsDocuments = expand != null && expand.Contains("projets_documents") ? p.ProjetsDocuments.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("projets_items") ? p.ProjetsItems.Take(20).ToList() : null,
                ProjetsProjetTags = expand != null && expand.Contains("projets_projet_tags") ? p.ProjetsProjetTags.Take(20).ToList() : null,
                ProjetsStatus = expand != null && expand.Contains("projets_status_history") ? p.ProjetsStatus.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Projet with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjetDto>(projet.Projet) with
        {
            date_debut_projet = projet.DateDebutProjet,
            date_fin_projet = projet.DateFinProjet,
            projets_commentaires_count = projet.ProjetsCommentairesCount,
            projets_documents_count = projet.ProjetsDocumentsCount,
            projets_items_count = projet.ProjetsItemsCount,
            projets_tags_count = projet.ProjetsProjetsTagsCount,
            projets_status_history_count = projet.ProjetsStatusHistoryCount,
            projets_commentaires = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(projet.ProjetsCommentaires),
            projets_documents = _mapper.Map<IEnumerable<ReadProjetDocumentDto>>(projet.ProjetsDocuments),
            projets_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(projet.ProjetsItems),
            projets_projet_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(projet.ProjetsProjetTags),
            projets_status_history = _mapper.Map<IEnumerable<ReadProjetStatusDto>>(projet.ProjetsStatus)
        };
    }

    public async Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto)
    {
        var newProjet = _mapper.Map<Projets>(projetDto);
        _context.Projets.Add(newProjet);
        await _fileService.CreateDirectory(Path.Combine(_projetDocumentsPath, newProjet.id_projet.ToString()));
        await _context.SaveChangesAsync();
        await _projetStatusService.CreateProjetStatus(new CreateProjetStatusDto
        {
            id_projet = newProjet.id_projet,
            status_projet = newProjet.status_projet
        });
        return _mapper.Map<ReadProjetDto>(newProjet);
    }

    public async Task<ReadProjetDto> UpdateProjet(int id, UpdateProjetDto projetDto)
    {
        var projetToUpdate = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id '{id}' not found");
        var statusChanged = projetDto.status_projet.HasValue && projetDto.status_projet.Value != projetToUpdate.status_projet;
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
        await _context.SaveChangesAsync();
        if (statusChanged)
        {
            await _projetStatusService.CreateProjetStatus(new CreateProjetStatusDto
            {
                id_projet = projetToUpdate.id_projet,
                status_projet = projetToUpdate.status_projet
            });
        }
        return _mapper.Map<ReadProjetDto>(projetToUpdate);
    }

    public async Task DeleteProjet(int id)
    {
        var projetToDelete = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id '{id}' not found");
        _context.Projets.Remove(projetToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_projetDocumentsPath, id.ToString()));
        await _context.SaveChangesAsync();
    }
}