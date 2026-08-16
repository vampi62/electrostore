using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjectDocumentService;

public class ProjectDocumentService : IProjectDocumentService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _projetDocumentsPath = "projetDocuments";

    public ProjectDocumentService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadProjectDocumentDto>> GetProjetDocumentsByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetId))
        {
            throw new KeyNotFoundException($"Project with id '{projetId}' not found");
        }
        var query = _context.ProjectsDocuments.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsDocuments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsDocuments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsDocuments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_project_document", Order = "asc" };
                query = query.OrderBy(pd => pd.id_project_document);
            }
        }
        else
        {
            query = query.OrderBy(pd => pd.id_project_document);
        }
        query = query.Skip(offset).Take(limit);
        var projetDocument = await query.ToListAsync();
        return new PaginatedResponseDto<ReadProjectDocumentDto>
        {
            data = _mapper.Map<List<ReadProjectDocumentDto>>(projetDocument),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsDocuments.CountAsync(filterResult ?? (pd => pd.id_project == projetId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsDocuments.Skip(offset + limit).AnyAsync(filterResult ?? (pd => pd.id_project == projetId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadProjectDocumentDto> GetProjetDocumentById(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjectsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found");
        if (projetId is not null && projetDocument.id_project != projetId)
        {
            throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found for project with id '{projetId}'");
        }
        return _mapper.Map<ReadProjectDocumentDto>(projetDocument);
    }

    public async Task<ReadProjectDocumentDto> CreateProjetDocument(CreateProjectDocumentDto projetDocumentDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetDocumentDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projetDocumentDto.id_project}' not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_projetDocumentsPath, projetDocumentDto.id_project.ToString()), projetDocumentDto.document.FileName, projetDocumentDto.document.ContentType, projetDocumentDto.document.OpenReadStream());
        var projetDocument = new ProjectsDocuments
        {
            id_project = projetDocumentDto.id_project,
            url_project_document = savedFile.path,
            name_project_document = projetDocumentDto.name_project_document,
            type_project_document = savedFile.mimeType,
            size_project_document = projetDocumentDto.document.Length
        };
        await _context.ProjectsDocuments.AddAsync(projetDocument);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectDocumentDto>(projetDocument);
    }

    public async Task<ReadProjectDocumentDto> UpdateProjetDocument(int id, UpdateProjectDocumentDto projetDocumentDto, int? projetId = null)
    {
        var projetDocument = await _context.ProjectsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found");
        if (projetId is not null && projetDocument.id_project != projetId)
        {
            throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found for project with id '{projetId}'");
        }
        if (projetDocumentDto.name_project_document is not null)
        {
            projetDocument.name_project_document = projetDocumentDto.name_project_document;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectDocumentDto>(projetDocument);
    }

    public async Task DeleteProjetDocument(int id, int? projetId = null)
    {
        var projetDocument = await _context.ProjectsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found");
        if (projetId is not null && projetDocument.id_project != projetId)
        {
            throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found for project with id '{projetId}'");
        }
        await _fileService.DeleteFile(projetDocument.url_project_document);
        _context.ProjectsDocuments.Remove(projetDocument);
        await _context.SaveChangesAsync();
    }
}