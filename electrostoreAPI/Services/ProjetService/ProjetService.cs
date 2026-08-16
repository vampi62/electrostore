using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ProjetStatusService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjetService;

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

    public async Task<PaginatedResponseDto<ReadExtendedProjetDto>> GetProjets(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Projets.AsQueryable();
        var filterResult = default(Expression<Func<Projets, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(p => idResearch.Contains(p.id_project));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Projets>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Projets>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_project", Order = "asc" };
                    query = query.OrderBy(p => p.id_project);
                }
            }
            else
            {
                query = query.OrderBy(p => p.id_project);
            }
        }
        query = query.Skip(offset).Take(limit);
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
                    .Where(ps => ps.status_project == ProjetStatus.InProgress)
                    .OrderBy(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                DateFinProjet = p.ProjetsStatus
                    .Where(ps => ps.status_project == ProjetStatus.Completed)
                    .OrderByDescending(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                ProjetsCommentaires = expand != null && expand.Contains("project_comments") ? p.ProjetsCommentaires.Take(20).ToList() : null,
                ProjetsDocuments = expand != null && expand.Contains("project_documents") ? p.ProjetsDocuments.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("project_items") ? p.ProjetsItems.Take(20).ToList() : null,
                ProjetsProjetTags = expand != null && expand.Contains("project_tags") ? p.ProjetsProjetTags.Take(20).ToList() : null,
                ProjetsStatus = expand != null && expand.Contains("project_status_history") ? p.ProjetsStatus.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjetDto>
        {
            data = projet.Select(p => {
                return _mapper.Map<ReadExtendedProjetDto>(p.Projet) with
                {
                    date_start_project = p.DateDebutProjet,
                    date_end_project = p.DateFinProjet,
                    project_comments_count = p.ProjetsCommentairesCount,
                    project_documents_count = p.ProjetsDocumentsCount,
                    project_items_count = p.ProjetsItemsCount,
                    project_tags_count = p.ProjetsProjetTagsCount,
                    project_status_history_count = p.ProjetsStatusHistoryCount,
                    project_comments = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(p.ProjetsCommentaires),
                    project_documents = _mapper.Map<IEnumerable<ReadProjetDocumentDto>>(p.ProjetsDocuments),
                    project_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(p.ProjetsItems),
                    project_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(p.ProjetsProjetTags),
                    project_status_history = _mapper.Map<IEnumerable<ReadProjetStatusDto>>(p.ProjetsStatus)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Projets.CountAsync(filterResult ?? (p => true)),
                nextOffset = offset + limit,
                hasMore = await _context.Projets.Skip(offset + limit).AnyAsync(filterResult ?? (p => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjetDto> GetProjetById(int id, List<string>? expand = null)
    {
        var query = _context.Projets.AsQueryable();
        query = query.Where(p => p.id_project == id);
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
                    .Where(ps => ps.status_project == ProjetStatus.InProgress)
                    .OrderBy(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                DateFinProjet = p.ProjetsStatus
                    .Where(ps => ps.status_project == ProjetStatus.Completed)
                    .OrderByDescending(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                ProjetsCommentaires = expand != null && expand.Contains("project_comments") ? p.ProjetsCommentaires.Take(20).ToList() : null,
                ProjetsDocuments = expand != null && expand.Contains("project_documents") ? p.ProjetsDocuments.Take(20).ToList() : null,
                ProjetsItems = expand != null && expand.Contains("project_items") ? p.ProjetsItems.Take(20).ToList() : null,
                ProjetsProjetTags = expand != null && expand.Contains("project_tags") ? p.ProjetsProjetTags.Take(20).ToList() : null,
                ProjetsStatus = expand != null && expand.Contains("project_status_history") ? p.ProjetsStatus.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Projet with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjetDto>(projet.Projet) with
        {
            date_start_project = projet.DateDebutProjet,
            date_end_project = projet.DateFinProjet,
            project_comments_count = projet.ProjetsCommentairesCount,
            project_documents_count = projet.ProjetsDocumentsCount,
            project_items_count = projet.ProjetsItemsCount,
            project_tags_count = projet.ProjetsProjetsTagsCount,
            project_status_history_count = projet.ProjetsStatusHistoryCount,
            project_comments = _mapper.Map<IEnumerable<ReadProjetCommentaireDto>>(projet.ProjetsCommentaires),
            project_documents = _mapper.Map<IEnumerable<ReadProjetDocumentDto>>(projet.ProjetsDocuments),
            project_items = _mapper.Map<IEnumerable<ReadProjetItemDto>>(projet.ProjetsItems),
            project_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(projet.ProjetsProjetTags),
            project_status_history = _mapper.Map<IEnumerable<ReadProjetStatusDto>>(projet.ProjetsStatus)
        };
    }

    public async Task<ReadProjetDto> CreateProjet(CreateProjetDto projetDto)
    {
        var newProjet = _mapper.Map<Projets>(projetDto);
        _context.Projets.Add(newProjet);
        await _fileService.CreateDirectory(Path.Combine(_projetDocumentsPath, newProjet.id_project.ToString()));
        await _context.SaveChangesAsync();
        await _projetStatusService.CreateProjetStatus(new CreateProjetStatusDto
        {
            id_project = newProjet.id_project,
            status_project = newProjet.status_project
        });
        return _mapper.Map<ReadProjetDto>(newProjet);
    }

    public async Task<ReadProjetDto> UpdateProjet(int id, UpdateProjetDto projetDto)
    {
        var projetToUpdate = await _context.Projets.FindAsync(id) ?? throw new KeyNotFoundException($"Projet with id '{id}' not found");
        var statusChanged = projetDto.status_project.HasValue && projetDto.status_project.Value != projetToUpdate.status_project;
        if (projetDto.name_project is not null)
        {
            projetToUpdate.name_project = projetDto.name_project;
        }
        if (projetDto.description_project is not null)
        {
            projetToUpdate.description_project = projetDto.description_project;
        }
        if (projetDto.url_project is not null)
        {
            projetToUpdate.url_project = projetDto.url_project;
        }
        if (projetDto.status_project is not null)
        {
            projetToUpdate.status_project = projetDto.status_project.Value;
        }
        await _context.SaveChangesAsync();
        if (statusChanged)
        {
            await _projetStatusService.CreateProjetStatus(new CreateProjetStatusDto
            {
                id_project = projetToUpdate.id_project,
                status_project = projetToUpdate.status_project
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