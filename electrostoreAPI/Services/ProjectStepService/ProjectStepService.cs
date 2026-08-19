using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjectStepService;

public class ProjectStepService : IProjectStepService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjectStepService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectStepDto>> GetProjectStepsByProjectId(int projectId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectId))
        {
            throw new KeyNotFoundException($"Project with id '{projectId}' not found");
        }
        var query = _context.ProjectsSteps.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsSteps, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projectId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsSteps>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsSteps>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "order_project_step", Order = "asc" };
                query = query.OrderBy(ps => ps.order_project_step);
            }
        }
        else
        {
            query = query.OrderBy(ps => ps.order_project_step);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(ps => ps.Project);
        }
        var projectStep = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectStepDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectStepDto>>(projectStep),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsSteps.CountAsync(filterResult ?? (ps => ps.id_project == projectId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsSteps.Skip(offset + limit).AnyAsync(filterResult ?? (ps => ps.id_project == projectId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectStepDto> GetProjectStepById(int id, int? projectId = null, List<string>? expand = null)
    {
        var query = _context.ProjectsSteps.AsQueryable();
        query = query.Where(ps => ps.id_project_step == id && (projectId == null || ps.id_project == projectId));
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(ps => ps.Project);
        }
        var projectStep = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectStep with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjectStepDto>(projectStep);
    }

    public async Task<ReadProjectStepDto> CreateProjectStep(CreateProjectStepDto projectStepDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectStepDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projectStepDto.id_project}' not found");
        }
        var newProjectStep = _mapper.Map<ProjectsSteps>(projectStepDto);
        _context.ProjectsSteps.Add(newProjectStep);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectStepDto>(newProjectStep);
    }

    public async Task<ReadProjectStepDto> UpdateProjectStep(int id, UpdateProjectStepDto projectStepDto, int? projectId = null)
    {
        var projectStepToUpdate = await _context.ProjectsSteps.FindAsync(id);
        if (projectStepToUpdate is null || (projectId is not null && projectStepToUpdate.id_project != projectId))
        {
            throw new KeyNotFoundException($"ProjectStep with id '{id}' not found");
        }
        if (projectStepDto.name_project_step is not null)
        {
            projectStepToUpdate.name_project_step = projectStepDto.name_project_step;
        }
        if (projectStepDto.description_project_step is not null)
        {
            projectStepToUpdate.description_project_step = projectStepDto.description_project_step;
        }
        if (projectStepDto.status_project_step is not null)
        {
            projectStepToUpdate.status_project_step = projectStepDto.status_project_step.Value;
        }
        if (projectStepDto.order_project_step is not null)
        {
            projectStepToUpdate.order_project_step = projectStepDto.order_project_step.Value;
        }
        if (projectStepDto.planned_start_project_step is not null)
        {
            projectStepToUpdate.planned_start_project_step = projectStepDto.planned_start_project_step;
        }
        if (projectStepDto.planned_end_project_step is not null)
        {
            projectStepToUpdate.planned_end_project_step = projectStepDto.planned_end_project_step;
        }
        if (projectStepDto.actual_start_project_step is not null)
        {
            projectStepToUpdate.actual_start_project_step = projectStepDto.actual_start_project_step;
        }
        if (projectStepDto.actual_end_project_step is not null)
        {
            projectStepToUpdate.actual_end_project_step = projectStepDto.actual_end_project_step;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectStepDto>(projectStepToUpdate);
    }

    public async Task DeleteProjectStep(int id, int? projectId = null)
    {
        var projectStepToDelete = await _context.ProjectsSteps.FindAsync(id);
        if (projectStepToDelete is null || (projectId is not null && projectStepToDelete.id_project != projectId))
        {
            throw new KeyNotFoundException($"ProjectStep with id '{id}' not found");
        }
        _context.ProjectsSteps.Remove(projectStepToDelete);
        await _context.SaveChangesAsync();
    }
}
