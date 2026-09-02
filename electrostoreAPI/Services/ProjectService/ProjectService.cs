using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.ProjectStatusService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjectService;

public class ProjectService : IProjectService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly IProjectStatusService _projectStatusService;
    private readonly string _projectDocumentsPath = "projectDocuments";

    public ProjectService(IMapper mapper, ApplicationDbContext context, IFileService fileService, IProjectStatusService projectStatusService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
        _projectStatusService = projectStatusService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectDto>> GetProjects(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Projects.AsQueryable();
        var filterResult = default(Expression<Func<Projects, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(p => idResearch.Contains(p.id_project));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Projects>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Projects>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { field = "id_project", order = "asc" };
                    query = query.OrderBy(p => p.id_project);
                }
            }
            else
            {
                query = query.OrderBy(p => p.id_project);
            }
        }
        query = query.Skip(offset).Take(limit);
        var project = await query
            .Select(p => new
            {
                Project = p,
                ProjectsCommentsCount = p.ProjectsComments.Count,
                ProjectsDocumentsCount = p.ProjectsDocuments.Count,
                ProjectsItemsCount = p.ProjectsItems.Count,
                ProjectsProjectTagsCount = p.ProjectsProjectTags.Count,
                ProjectsStatusHistoryCount = p.ProjectsStatus.Count,
                DateStartProject = p.ProjectsStatus
                    .Where(ps => ps.status_project == ProjectStatus.InProgress)
                    .OrderBy(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                DateEndProject = p.ProjectsStatus
                    .Where(ps => ps.status_project == ProjectStatus.Completed)
                    .OrderByDescending(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                ProjectsComments = expand != null && expand.Contains("project_comments") ? p.ProjectsComments.Take(20).ToList() : null,
                ProjectsDocuments = expand != null && expand.Contains("project_documents") ? p.ProjectsDocuments.Take(20).ToList() : null,
                ProjectsItems = expand != null && expand.Contains("project_items") ? p.ProjectsItems.Take(20).ToList() : null,
                ProjectsProjectTags = expand != null && expand.Contains("project_tags") ? p.ProjectsProjectTags.Take(20).ToList() : null,
                ProjectsStatus = expand != null && expand.Contains("project_status_history") ? p.ProjectsStatus.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectDto>
        {
            data = project.Select(p => {
                return _mapper.Map<ReadExtendedProjectDto>(p.Project) with
                {
                    date_start_project = p.DateStartProject,
                    date_end_project = p.DateEndProject,
                    project_comments_count = p.ProjectsCommentsCount,
                    project_documents_count = p.ProjectsDocumentsCount,
                    project_items_count = p.ProjectsItemsCount,
                    project_tags_count = p.ProjectsProjectTagsCount,
                    project_status_history_count = p.ProjectsStatusHistoryCount,
                    project_comments = _mapper.Map<IEnumerable<ReadProjectCommentDto>>(p.ProjectsComments),
                    project_documents = _mapper.Map<IEnumerable<ReadProjectDocumentDto>>(p.ProjectsDocuments),
                    project_items = _mapper.Map<IEnumerable<ReadProjectItemDto>>(p.ProjectsItems),
                    project_tags = _mapper.Map<IEnumerable<ReadProjectProjectTagDto>>(p.ProjectsProjectTags),
                    project_status_history = _mapper.Map<IEnumerable<ReadProjectStatusDto>>(p.ProjectsStatus)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Projects.CountAsync(filterResult ?? (p => true)),
                next_offset = offset + limit,
                has_more = await _context.Projects.Skip(offset + limit).AnyAsync(filterResult ?? (p => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectDto> GetProjectById(int id, List<string>? expand = null)
    {
        var query = _context.Projects.AsQueryable();
        query = query.Where(p => p.id_project == id);
        var project = await query
            .Select(p => new
            {
                Project = p,
                ProjectsCommentsCount = p.ProjectsComments.Count,
                ProjectsDocumentsCount = p.ProjectsDocuments.Count,
                ProjectsItemsCount = p.ProjectsItems.Count,
                ProjectsProjectsTagsCount = p.ProjectsProjectTags.Count,
                ProjectsStatusHistoryCount = p.ProjectsStatus.Count,
                DateStartProject = p.ProjectsStatus
                    .Where(ps => ps.status_project == ProjectStatus.InProgress)
                    .OrderBy(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                DateEndProject = p.ProjectsStatus
                    .Where(ps => ps.status_project == ProjectStatus.Completed)
                    .OrderByDescending(ps => ps.created_at)
                    .Select(ps => (DateTime?)ps.created_at)
                    .FirstOrDefault(),
                ProjectsComments = expand != null && expand.Contains("project_comments") ? p.ProjectsComments.Take(20).ToList() : null,
                ProjectsDocuments = expand != null && expand.Contains("project_documents") ? p.ProjectsDocuments.Take(20).ToList() : null,
                ProjectsItems = expand != null && expand.Contains("project_items") ? p.ProjectsItems.Take(20).ToList() : null,
                ProjectsProjectTags = expand != null && expand.Contains("project_tags") ? p.ProjectsProjectTags.Take(20).ToList() : null,
                ProjectsStatus = expand != null && expand.Contains("project_status_history") ? p.ProjectsStatus.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Project with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjectDto>(project.Project) with
        {
            date_start_project = project.DateStartProject,
            date_end_project = project.DateEndProject,
            project_comments_count = project.ProjectsCommentsCount,
            project_documents_count = project.ProjectsDocumentsCount,
            project_items_count = project.ProjectsItemsCount,
            project_tags_count = project.ProjectsProjectsTagsCount,
            project_status_history_count = project.ProjectsStatusHistoryCount,
            project_comments = _mapper.Map<IEnumerable<ReadProjectCommentDto>>(project.ProjectsComments),
            project_documents = _mapper.Map<IEnumerable<ReadProjectDocumentDto>>(project.ProjectsDocuments),
            project_items = _mapper.Map<IEnumerable<ReadProjectItemDto>>(project.ProjectsItems),
            project_tags = _mapper.Map<IEnumerable<ReadProjectProjectTagDto>>(project.ProjectsProjectTags),
            project_status_history = _mapper.Map<IEnumerable<ReadProjectStatusDto>>(project.ProjectsStatus)
        };
    }

    public async Task<ReadProjectDto> CreateProject(CreateProjectDto projectDto)
    {
        var newProject = _mapper.Map<Projects>(projectDto);
        _context.Projects.Add(newProject);
        await _fileService.CreateDirectory(Path.Combine(_projectDocumentsPath, newProject.id_project.ToString()));
        await _context.SaveChangesAsync();
        await _projectStatusService.CreateProjectStatus(new CreateProjectStatusDto
        {
            id_project = newProject.id_project,
            status_project = newProject.status_project
        });
        return _mapper.Map<ReadProjectDto>(newProject);
    }

    public async Task<ReadProjectDto> UpdateProject(int id, UpdateProjectDto projectDto)
    {
        var projectToUpdate = await _context.Projects.FindAsync(id) ?? throw new KeyNotFoundException($"Project with id '{id}' not found");
        var statusChanged = projectDto.status_project.HasValue && projectDto.status_project.Value != projectToUpdate.status_project;
        if (projectDto.name_project is not null)
        {
            projectToUpdate.name_project = projectDto.name_project;
        }
        if (projectDto.description_project is not null)
        {
            projectToUpdate.description_project = projectDto.description_project;
        }
        if (projectDto.url_project is not null)
        {
            projectToUpdate.url_project = projectDto.url_project;
        }
        if (projectDto.status_project is not null)
        {
            projectToUpdate.status_project = projectDto.status_project.Value;
        }
        await _context.SaveChangesAsync();
        if (statusChanged)
        {
            await _projectStatusService.CreateProjectStatus(new CreateProjectStatusDto
            {
                id_project = projectToUpdate.id_project,
                status_project = projectToUpdate.status_project
            });
        }
        return _mapper.Map<ReadProjectDto>(projectToUpdate);
    }

    public async Task DeleteProject(int id)
    {
        var projectToDelete = await _context.Projects.FindAsync(id) ?? throw new KeyNotFoundException($"Project with id '{id}' not found");
        _context.Projects.Remove(projectToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_projectDocumentsPath, id.ToString()));
        await _context.SaveChangesAsync();
    }
}