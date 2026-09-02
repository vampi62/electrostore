using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjectProjectTagService;

public class ProjectProjectTagService : IProjectProjectTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public ProjectProjectTagService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjectsProjectTagsByProjectId(int projectId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if project exists
        if (!await _context.Projects.AnyAsync(s => s.id_project == projectId))
        {
            throw new KeyNotFoundException($"Project with id '{projectId}' not found");
        }
        var query = _context.ProjectsProjectTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsProjectTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_project", search_type = "eq", value = projectId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsProjectTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsProjectTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_project_tag", order = "asc" };
                query = query.OrderBy(st => st.id_project_tag);
            }
        }
        else
        {
            query = query.OrderBy(st => st.id_project_tag);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(st => st.Project);
        }
        if (expand != null && expand.Contains("project_tag"))
        {
            query = query.Include(st => st.ProjectTag);
        }
        var projectProjectTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectProjectTagDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectProjectTagDto>>(projectProjectTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsProjectTags.CountAsync(filterResult ?? (st => st.id_project == projectId)),
                next_offset = offset + limit,
                has_more = await _context.ProjectsProjectTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_project == projectId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjectsProjectTagsByprojectTagId(int projectTagId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if projectTag exists
        if (!await _context.ProjectTags.AnyAsync(t => t.id_project_tag == projectTagId))
        {
            throw new KeyNotFoundException($"ProjectTag with id '{projectTagId}' not found");
        }
        var query = _context.ProjectsProjectTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsProjectTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_project_tag", search_type = "eq", value = projectTagId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsProjectTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsProjectTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_project", order = "asc" };
                query = query.OrderBy(st => st.id_project);
            }
        }
        else
        {
            query = query.OrderBy(st => st.id_project);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("project_tag"))
        {
            query = query.Include(st => st.ProjectTag);
        }
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(st => st.Project);
        }
        var projectProjectTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectProjectTagDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectProjectTagDto>>(projectProjectTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsProjectTags.CountAsync(filterResult ?? (st => st.id_project_tag == projectTagId)),
                next_offset = offset + limit,
                has_more = await _context.ProjectsProjectTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_project_tag == projectTagId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectProjectTagDto> GetProjectProjectTagById(int projectId, int projectTagId, List<string>? expand = null)
    {
        var query = _context.ProjectsProjectTags.AsQueryable();
        query = query.Where(st => st.id_project == projectId && st.id_project_tag == projectTagId);
        if (expand != null && expand.Contains("project_tag"))
        {
            query = query.Include(st => st.ProjectTag);
        }
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(st => st.Project);
        }
        var projectProjectTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectProjectTag with projectId '{projectId}' and projectTagId '{projectTagId}' not found");
        return _mapper.Map<ReadExtendedProjectProjectTagDto>(projectProjectTag);
    }

    public async Task<ReadProjectProjectTagDto> CreateProjectProjectTag(CreateProjectProjectTagDto projectProjectTagDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create ProjectProjectTag");
        }
        // check if store exists
        if (!await _context.Projects.AnyAsync(s => s.id_project == projectProjectTagDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projectProjectTagDto.id_project}' not found");
        }
        // check if tag exists
        if (!await _context.ProjectTags.AnyAsync(t => t.id_project_tag == projectProjectTagDto.id_project_tag))
        {
            throw new KeyNotFoundException($"Tag with id '{projectProjectTagDto.id_project_tag}' not found");
        }
        // check if store tag already exists
        if (await _context.ProjectsProjectTags.AnyAsync(st => st.id_project == projectProjectTagDto.id_project && st.id_project_tag == projectProjectTagDto.id_project_tag))
        {
            throw new InvalidOperationException($"ProjectProjectTag with projectId '{projectProjectTagDto.id_project}' and projectTagId '{projectProjectTagDto.id_project_tag}' already exists");
        }
        var newProjectProjectTag = _mapper.Map<ProjectsProjectTags>(projectProjectTagDto);
        _context.ProjectsProjectTags.Add(newProjectProjectTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectProjectTagDto>(newProjectProjectTag);
    }

    public async Task<ReadBulkProjectProjectTagDto> CreateBulkProjectProjectTag(List<CreateProjectProjectTagDto> projectProjectTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create ProjectProjectTag");
        }
        var validQuery = new List<ReadProjectProjectTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projectProjectTagDto in projectProjectTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjectProjectTag(projectProjectTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    reason = e.Message,
                    data = projectProjectTagDto
                });
            }
        }
        return new ReadBulkProjectProjectTagDto
        {
            valide = validQuery,
            error = errorQuery
        };
    }

    public async Task DeleteProjectProjectTag(int projectId, int projectTagId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete ProjectProjectTag");
        }
        var projectProjectTag = await _context.ProjectsProjectTags.FindAsync(projectId, projectTagId) ?? throw new KeyNotFoundException($"ProjectProjectTag with projectId '{projectId}' and projectTagId '{projectTagId}' not found");
        _context.ProjectsProjectTags.Remove(projectProjectTag);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkProjectProjectTagDto> DeleteBulkProjectProjectTag(List<CreateProjectProjectTagDto> projectProjectTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete ProjectProjectTag");
        }
        var validQuery = new List<ReadProjectProjectTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projectProjectTagDto in projectProjectTagBulkDto)
        {
            try
            {
                await DeleteProjectProjectTag(projectProjectTagDto.id_project, projectProjectTagDto.id_project_tag);
                validQuery.Add(new ReadProjectProjectTagDto
                {
                    id_project = projectProjectTagDto.id_project,
                    id_project_tag = projectProjectTagDto.id_project_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    reason = e.Message,
                    data = projectProjectTagDto
                });
            }
        }
        await _context.SaveChangesAsync();
        return new ReadBulkProjectProjectTagDto
        {
            valide = validQuery,
            error = errorQuery
        };
    }
}