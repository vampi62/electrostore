using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjetDocumentService;

public class ProjetDocumentService : IProjetDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _projetDocumentsPath = "projetDocuments";
    private readonly ILogger<ProjetDocumentService> _logger;

    public ProjetDocumentService(IMapper mapper, ApplicationDbContext context, IFileService fileService, ILogger<ProjetDocumentService> logger)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
        _logger = logger;
    }

    public async Task<PaginatedResponseDto<ReadProjetDocumentDto>> GetProjetDocumentsByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        _logger.LogDebug("GetProjetDocumentsByProjetId: projetId={ProjetId}, limit={Limit}, offset={Offset}", projetId, limit, offset);
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            _logger.LogWarning("GetProjetDocumentsByProjetId: projet {ProjetId} not found", projetId);
            throw new KeyNotFoundException($"Projet with id '{projetId}' not found");
        }
        var query = _context.ProjetsDocuments.AsQueryable();
        var filterResult = default(Expression<Func<ProjetsDocuments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_projet", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjetsDocuments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjetsDocuments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_projet_document", Order = "asc" };
                query = query.OrderBy(pd => pd.id_projet_document);
            }
        }
        else
        {
            query = query.OrderBy(pd => pd.id_projet_document);
        }
        query = query.Skip(offset).Take(limit);
        var projetDocument = await query.ToListAsync();
        return new PaginatedResponseDto<ReadProjetDocumentDto>
        {
            data = _mapper.Map<List<ReadProjetDocumentDto>>(projetDocument),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjetsDocuments.CountAsync(filterResult ?? (pd => pd.id_projet == projetId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjetsDocuments.Skip(offset + limit).AnyAsync(filterResult ?? (pd => pd.id_projet == projetId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadProjetDocumentDto> GetProjetDocumentById(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id);
        if (projetDocument is null)
        {
            _logger.LogWarning("GetProjetDocumentById: projet document {ProjetDocumentId} not found", id);
            throw new KeyNotFoundException($"ProjetDocument with id '{id}' not found");
        }
        if (projetId is not null && projetDocument.id_projet != projetId)
        {
            _logger.LogWarning("GetProjetDocumentById: projet document {ProjetDocumentId} not found for projet {ProjetId}", id, projetId);
            throw new KeyNotFoundException($"ProjetDocument with id '{id}' not found for projet with id '{projetId}'");
        }
        return _mapper.Map<ReadProjetDocumentDto>(projetDocument);
    }

    public async Task<ReadProjetDocumentDto> CreateProjetDocument(CreateProjetDocumentDto projetDocumentDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetDocumentDto.id_projet))
        {
            _logger.LogWarning("CreateProjetDocument: projet {ProjetId} not found", projetDocumentDto.id_projet);
            throw new KeyNotFoundException($"Projet with id '{projetDocumentDto.id_projet}' not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_projetDocumentsPath, projetDocumentDto.id_projet.ToString()), projetDocumentDto.document.FileName, projetDocumentDto.document.ContentType, projetDocumentDto.document.OpenReadStream());
        var projetDocument = new ProjetsDocuments
        {
            id_projet = projetDocumentDto.id_projet,
            url_projet_document = savedFile.path,
            name_projet_document = projetDocumentDto.name_projet_document,
            type_projet_document = savedFile.mimeType,
            size_projet_document = projetDocumentDto.document.Length
        };
        await _context.ProjetsDocuments.AddAsync(projetDocument);
        await _context.SaveChangesAsync();
        _logger.LogInformation("ProjetDocument {ProjetDocumentId} created for projet {ProjetId}", projetDocument.id_projet_document, projetDocument.id_projet);
        return _mapper.Map<ReadProjetDocumentDto>(projetDocument);
    }

    public async Task<ReadProjetDocumentDto> UpdateProjetDocument(int id, UpdateProjetDocumentDto projetDocumentDto, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id);
        if (projetDocument is null)
        {
            _logger.LogWarning("UpdateProjetDocument: projet document {ProjetDocumentId} not found", id);
            throw new KeyNotFoundException($"ProjetDocument with id '{id}' not found");
        }
        if (projetId is not null && projetDocument.id_projet != projetId)
        {
            _logger.LogWarning("UpdateProjetDocument: projet document {ProjetDocumentId} not found for projet {ProjetId}", id, projetId);
            throw new KeyNotFoundException($"ProjetDocument with id '{id}' not found for projet with id '{projetId}'");
        }
        if (projetDocumentDto.name_projet_document is not null)
        {
            projetDocument.name_projet_document = projetDocumentDto.name_projet_document;
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("ProjetDocument {ProjetDocumentId} updated", id);
        return _mapper.Map<ReadProjetDocumentDto>(projetDocument);
    }

    public async Task DeleteProjetDocument(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjetsDocuments.FindAsync(id);
        if (projetDocument is null)
        {
            _logger.LogWarning("DeleteProjetDocument: projet document {ProjetDocumentId} not found", id);
            throw new KeyNotFoundException($"ProjetDocument with id '{id}' not found");
        }
        if (projetId is not null && projetDocument.id_projet != projetId)
        {
            _logger.LogWarning("DeleteProjetDocument: projet document {ProjetDocumentId} not found for projet {ProjetId}", id, projetId);
            throw new KeyNotFoundException($"ProjetDocument with id '{id}' not found for projet with id '{projetId}'");
        }
        await _fileService.DeleteFile(projetDocument.url_projet_document);
        _context.ProjetsDocuments.Remove(projetDocument);
        await _context.SaveChangesAsync();
        _logger.LogInformation("ProjetDocument {ProjetDocumentId} deleted", id);
    }
}