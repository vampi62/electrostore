using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjectItemService;

public class ProjectItemService : IProjectItemService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjectItemService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjectItemsByProjectId(int projectId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectId))
        {
            throw new KeyNotFoundException($"Project with id '{projectId}' not found");
        }
        var query = _context.ProjectsItems.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsItems, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_project", search_type = "eq", value = projectId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsItems>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsItems>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_item", order = "asc" };
                query = query.OrderBy(pi => pi.id_item);
            }
        }
        else
        {
            query = query.OrderBy(pi => pi.id_item);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(pi => pi.Item);
        }
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(pi => pi.Project);
        }
        var projectItem = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectItemDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectItemDto>>(projectItem),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsItems.CountAsync(filterResult ?? (pi => pi.id_project == projectId)),
                next_offset = offset + limit,
                has_more = await _context.ProjectsItems.Skip(offset + limit).AnyAsync(filterResult ?? (pi => pi.id_project == projectId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjectItemsByItemId(int itemId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ProjectsItems.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsItems, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_item", search_type = "eq", value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsItems>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsItems>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_project", order = "asc" };
                query = query.OrderBy(pi => pi.id_project);
            }
        }
        else
        {
            query = query.OrderBy(pi => pi.id_project);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(pi => pi.Item);
        }
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(pi => pi.Project);
        }
        var projectItem = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectItemDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectItemDto>>(projectItem),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsItems.CountAsync(filterResult ?? (pi => pi.id_item == itemId)),
                next_offset = offset + limit,
                has_more = await _context.ProjectsItems.Skip(offset + limit).AnyAsync(filterResult ?? (pi => pi.id_item == itemId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectItemDto> GetProjectItemById(int projectId, int itemId, List<string>? expand = null)
    {
        var query = _context.ProjectsItems.AsQueryable();
        query = query.Where(pi => pi.id_project == projectId && pi.id_item == itemId);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(pi => pi.Item);
        }
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(pi => pi.Project);
        }
        var projectItem = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectItem with id_project '{projectId}' and id_item '{itemId}' not found");
        return _mapper.Map<ReadExtendedProjectItemDto>(projectItem);
    }

    public async Task<ReadProjectItemDto> CreateProjectItem(CreateProjectItemDto projectItemDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectItemDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projectItemDto.id_project}' not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == projectItemDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{projectItemDto.id_item}' not found");
        }
        // check if the projectItem already exists
        if (await _context.ProjectsItems.AnyAsync(pi => pi.id_project == projectItemDto.id_project && pi.id_item == projectItemDto.id_item))
        {
            throw new InvalidOperationException($"ProjectItem with id_project '{projectItemDto.id_project}' and id_item '{projectItemDto.id_item}' already exists");
        }
        var newProjectItem = _mapper.Map<ProjectsItems>(projectItemDto);
        _context.ProjectsItems.Add(newProjectItem);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectItemDto>(newProjectItem);
    }

    public async Task<ReadBulkProjectItemDto> CreateBulkProjectItem(List<CreateProjectItemDto> projectItemBulkDto)
    {
        var validQuery = new List<ReadProjectItemDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projectItemDto in projectItemBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjectItem(projectItemDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    reason = e.Message,
                    data = projectItemDto
                });
            }
        }
        return new ReadBulkProjectItemDto
        {
            valide = validQuery,
            error = errorQuery
        };
    }

    public async Task<ReadProjectItemDto> UpdateProjectItem(int projectId, int itemId, UpdateProjectItemDto projectItemDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projectId))
        {
            throw new KeyNotFoundException($"Project with id '{projectId}' not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var projectItemToUpdate = await _context.ProjectsItems.FindAsync(projectId, itemId) ?? throw new KeyNotFoundException($"ProjectItem with id_project '{projectId}' and id_item '{itemId}' not found");
        if (projectItemDto.quantity_project_item is not null)
        {
            projectItemToUpdate.quantity_project_item = projectItemDto.quantity_project_item.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectItemDto>(projectItemToUpdate);
    }

    public async Task DeleteProjectItem(int projectId, int itemId)
    {
        var projectItemToDelete = await _context.ProjectsItems.FindAsync(projectId, itemId) ?? throw new KeyNotFoundException($"ProjectItem with id_project '{projectId}' and id_item '{itemId}' not found");
        _context.ProjectsItems.Remove(projectItemToDelete);
        await _context.SaveChangesAsync();
    }
}