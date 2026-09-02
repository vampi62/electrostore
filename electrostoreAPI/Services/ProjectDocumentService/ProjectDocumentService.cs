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
    private readonly string _projectDocumentsPath = "projectDocuments";

    public ProjectDocumentService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadProjectDocumentDto>> GetProjectDocumentsByProjectId(int projectId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectId))
        {
            throw new KeyNotFoundException($"Project with id '{projectId}' not found");
        }
        var query = _context.ProjectsDocuments.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsDocuments, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_project", search_type = "eq", value = projectId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsDocuments>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsDocuments>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_project_document", order = "asc" };
                query = query.OrderBy(pd => pd.id_project_document);
            }
        }
        else
        {
            query = query.OrderBy(pd => pd.id_project_document);
        }
        query = query.Skip(offset).Take(limit);
        var projectDocument = await query.ToListAsync();
        return new PaginatedResponseDto<ReadProjectDocumentDto>
        {
            data = _mapper.Map<List<ReadProjectDocumentDto>>(projectDocument),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsDocuments.CountAsync(filterResult ?? (pd => pd.id_project == projectId)),
                next_offset = offset + limit,
                has_more = await _context.ProjectsDocuments.Skip(offset + limit).AnyAsync(filterResult ?? (pd => pd.id_project == projectId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadProjectDocumentDto> GetProjectDocumentById(int id, int? projectId = null)
    {
        var projectDocument = await _context.ProjectsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found");
        if (projectId is not null && projectDocument.id_project != projectId)
        {
            throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found for project with id '{projectId}'");
        }
        return _mapper.Map<ReadProjectDocumentDto>(projectDocument);
    }

    public async Task<ReadProjectDocumentDto> CreateProjectDocument(CreateProjectDocumentDto projectDocumentDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectDocumentDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projectDocumentDto.id_project}' not found");
        }
        var savedFile = await _fileService.SaveFile(Path.Combine(_projectDocumentsPath, projectDocumentDto.id_project.ToString()), projectDocumentDto.document.FileName, projectDocumentDto.document.ContentType, projectDocumentDto.document.OpenReadStream());
        var projectDocument = new ProjectsDocuments
        {
            id_project = projectDocumentDto.id_project,
            url_project_document = savedFile.path,
            name_project_document = projectDocumentDto.name_project_document,
            type_project_document = savedFile.mime_type,
            size_project_document = projectDocumentDto.document.Length
        };
        await _context.ProjectsDocuments.AddAsync(projectDocument);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectDocumentDto>(projectDocument);
    }

    public async Task<ReadProjectDocumentDto> UpdateProjectDocument(int id, UpdateProjectDocumentDto projectDocumentDto, int? projectId = null)
    {
        var projectDocument = await _context.ProjectsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found");
        if (projectId is not null && projectDocument.id_project != projectId)
        {
            throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found for project with id '{projectId}'");
        }
        if (projectDocumentDto.name_project_document is not null)
        {
            projectDocument.name_project_document = projectDocumentDto.name_project_document;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectDocumentDto>(projectDocument);
    }

    public async Task DeleteProjectDocument(int id, int? projectId = null)
    {
        var projectDocument = await _context.ProjectsDocuments.FindAsync(id) ?? throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found");
        if (projectId is not null && projectDocument.id_project != projectId)
        {
            throw new KeyNotFoundException($"ProjectDocument with id '{id}' not found for project with id '{projectId}'");
        }
        await _fileService.DeleteFile(projectDocument.url_project_document);
        _context.ProjectsDocuments.Remove(projectDocument);
        await _context.SaveChangesAsync();
    }
}