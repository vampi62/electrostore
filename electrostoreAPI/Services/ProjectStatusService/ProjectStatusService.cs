using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.ProjectStatusService;

public class ProjectStatusService : IProjectStatusService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjectStatusService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectStatusDto>> GetProjectStatusByProjectId(int projectId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectId))
        {
            throw new KeyNotFoundException($"Project with id '{projectId}' not found");
        }
        var query = _context.ProjectsStatus.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_project", search_type = "eq", value = projectId.ToString() });
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "created_at", order = "desc" })
            .ToResponseAsync(projectStatus => _mapper.Map<List<ReadExtendedProjectStatusDto>>(projectStatus));
    }

    public async Task<ReadExtendedProjectStatusDto> GetProjectStatusById(int id, int? projectId = null)
    {
        var query = _context.ProjectsStatus.AsQueryable();
        query = query.Where(pc => pc.id_project_status == id && (projectId == null || pc.id_project == projectId));
        var projectStatus = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectStatus with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjectStatusDto>(projectStatus);
    }

    public async Task<ReadProjectStatusDto> CreateProjectStatus(CreateProjectStatusDto projectStatusDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectStatusDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projectStatusDto.id_project}' not found");
        }
        var newProjectStatus = _mapper.Map<ProjectsStatus>(projectStatusDto);
        _context.ProjectsStatus.Add(newProjectStatus);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectStatusDto>(newProjectStatus);
    }
}