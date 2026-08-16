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

    public async Task<PaginatedResponseDto<ReadExtendedProjectStatusDto>> GetProjetStatusByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetId))
        {
            throw new KeyNotFoundException($"Project with id '{projetId}' not found");
        }
        var query = _context.ProjectsStatus.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            var filterResult = RsqlParserExtensions.ToFilterExpression<ProjectsStatus>(rsql);
            query = query.Where(filterResult.Item1);
            rsql = filterResult.Item2;
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsStatus>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(p => p.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(p => p.created_at);
        }
        query = query.Skip(offset).Take(limit);
        var projetStatus = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectStatusDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectStatusDto>>(projetStatus),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsStatus.CountAsync(p => p.id_project == projetId),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsStatus.Skip(offset + limit).AnyAsync(p => p.id_project == projetId)
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectStatusDto> GetProjetStatusById(int id, int? projetId = null)
    {
        var query = _context.ProjectsStatus.AsQueryable();
        query = query.Where(pc => pc.id_project_status == id && (projetId == null || pc.id_project == projetId));
        var projetStatus = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectStatus with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjectStatusDto>(projetStatus);
    }

    public async Task<ReadProjectStatusDto> CreateProjetStatus(CreateProjectStatusDto projetStatusDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetStatusDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projetStatusDto.id_project}' not found");
        }
        var newProjetStatus = _mapper.Map<ProjectsStatus>(projetStatusDto);
        _context.ProjectsStatus.Add(newProjetStatus);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectStatusDto>(newProjetStatus);
    }
}