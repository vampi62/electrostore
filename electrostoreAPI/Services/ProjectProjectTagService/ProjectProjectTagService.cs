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

    public async Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjetsProjetTagsByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if project exists
        if (!await _context.Projects.AnyAsync(s => s.id_project == projetId))
        {
            throw new KeyNotFoundException($"Project with id '{projetId}' not found");
        }
        var query = _context.ProjectsProjectTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsProjectTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsProjectTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsProjectTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_project_tag", Order = "asc" };
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
        var projetProjetTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectProjectTagDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectProjectTagDto>>(projetProjetTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsProjectTags.CountAsync(filterResult ?? (st => st.id_project == projetId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsProjectTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_project == projetId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectProjectTagDto>> GetProjetsProjetTagsByprojetTagId(int projetTagId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if projetTag exists
        if (!await _context.ProjectTags.AnyAsync(t => t.id_project_tag == projetTagId))
        {
            throw new KeyNotFoundException($"ProjectTag with id '{projetTagId}' not found");
        }
        var query = _context.ProjectsProjectTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsProjectTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project_tag", SearchType = "eq", Value = projetTagId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsProjectTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsProjectTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_project", Order = "asc" };
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
        var projetProjetTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectProjectTagDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectProjectTagDto>>(projetProjetTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsProjectTags.CountAsync(filterResult ?? (st => st.id_project_tag == projetTagId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsProjectTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_project_tag == projetTagId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectProjectTagDto> GetProjetProjetTagById(int projetId, int projetTagId, List<string>? expand = null)
    {
        var query = _context.ProjectsProjectTags.AsQueryable();
        query = query.Where(st => st.id_project == projetId && st.id_project_tag == projetTagId);
        if (expand != null && expand.Contains("project_tag"))
        {
            query = query.Include(st => st.ProjectTag);
        }
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(st => st.Project);
        }
        var projetProjetTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectProjectTag with projetId '{projetId}' and projetTagId '{projetTagId}' not found");
        return _mapper.Map<ReadExtendedProjectProjectTagDto>(projetProjetTag);
    }

    public async Task<ReadProjectProjectTagDto> CreateProjetProjetTag(CreateProjectProjectTagDto projetProjetTagDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create ProjectProjectTag");
        }
        // check if store exists
        if (!await _context.Projects.AnyAsync(s => s.id_project == projetProjetTagDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projetProjetTagDto.id_project}' not found");
        }
        // check if tag exists
        if (!await _context.ProjectTags.AnyAsync(t => t.id_project_tag == projetProjetTagDto.id_project_tag))
        {
            throw new KeyNotFoundException($"Tag with id '{projetProjetTagDto.id_project_tag}' not found");
        }
        // check if store tag already exists
        if (await _context.ProjectsProjectTags.AnyAsync(st => st.id_project == projetProjetTagDto.id_project && st.id_project_tag == projetProjetTagDto.id_project_tag))
        {
            throw new InvalidOperationException($"ProjectProjectTag with projetId '{projetProjetTagDto.id_project}' and projetTagId '{projetProjetTagDto.id_project_tag}' already exists");
        }
        var newProjetProjetTag = _mapper.Map<ProjectsProjectTags>(projetProjetTagDto);
        _context.ProjectsProjectTags.Add(newProjetProjetTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectProjectTagDto>(newProjetProjetTag);
    }

    public async Task<ReadBulkProjectProjectTagDto> CreateBulkProjetProjetTag(List<CreateProjectProjectTagDto> projetProjetTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create ProjectProjectTag");
        }
        var validQuery = new List<ReadProjectProjectTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projetProjetTagDto in projetProjetTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjetProjetTag(projetProjetTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projetProjetTagDto
                });
            }
        }
        return new ReadBulkProjectProjectTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteProjetProjetTag(int projetId, int projetTagId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete ProjectProjectTag");
        }
        var projetProjetTag = await _context.ProjectsProjectTags.FindAsync(projetId, projetTagId) ?? throw new KeyNotFoundException($"ProjectProjectTag with projetId '{projetId}' and projetTagId '{projetTagId}' not found");
        _context.ProjectsProjectTags.Remove(projetProjetTag);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkProjectProjectTagDto> DeleteBulkProjetProjetTag(List<CreateProjectProjectTagDto> projetProjetTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete ProjectProjectTag");
        }
        var validQuery = new List<ReadProjectProjectTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projetProjetTagDto in projetProjetTagBulkDto)
        {
            try
            {
                await DeleteProjetProjetTag(projetProjetTagDto.id_project, projetProjetTagDto.id_project_tag);
                validQuery.Add(new ReadProjectProjectTagDto
                {
                    id_project = projetProjetTagDto.id_project,
                    id_project_tag = projetProjetTagDto.id_project_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projetProjetTagDto
                });
            }
        }
        await _context.SaveChangesAsync();
        return new ReadBulkProjectProjectTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}