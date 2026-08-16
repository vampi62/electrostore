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

    public async Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjetItemsByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetId))
        {
            throw new KeyNotFoundException($"Project with id '{projetId}' not found");
        }
        var query = _context.ProjectsItems.AsQueryable();
        var filterResult = default(Expression<Func<ProjectsItems, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsItems>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsItems>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_item", Order = "asc" };
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
        var projetItem = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectItemDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectItemDto>>(projetItem),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsItems.CountAsync(filterResult ?? (pi => pi.id_project == projetId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsItems.Skip(offset + limit).AnyAsync(filterResult ?? (pi => pi.id_project == projetId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjectItemDto>> GetProjetItemsByItemId(int itemId, int limit = 100, int offset = 0,
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
        rsql.Add(new FilterDto { Field = "id_item", SearchType = "eq", Value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjectsItems>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjectsItems>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_project", Order = "asc" };
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
        var projetItem = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjectItemDto>
        {
            data = _mapper.Map<List<ReadExtendedProjectItemDto>>(projetItem),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjectsItems.CountAsync(filterResult ?? (pi => pi.id_item == itemId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjectsItems.Skip(offset + limit).AnyAsync(filterResult ?? (pi => pi.id_item == itemId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjectItemDto> GetProjetItemById(int projetId, int itemId, List<string>? expand = null)
    {
        var query = _context.ProjectsItems.AsQueryable();
        query = query.Where(pi => pi.id_project == projetId && pi.id_item == itemId);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(pi => pi.Item);
        }
        if (expand != null && expand.Contains("project"))
        {
            query = query.Include(pi => pi.Project);
        }
        var projetItem = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjectItem with id_project '{projetId}' and id_item '{itemId}' not found");
        return _mapper.Map<ReadExtendedProjectItemDto>(projetItem);
    }

    public async Task<ReadProjectItemDto> CreateProjetItem(CreateProjectItemDto projetItemDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetItemDto.id_project))
        {
            throw new KeyNotFoundException($"Project with id '{projetItemDto.id_project}' not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == projetItemDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{projetItemDto.id_item}' not found");
        }
        // check if the projetItem already exists
        if (await _context.ProjectsItems.AnyAsync(pi => pi.id_project == projetItemDto.id_project && pi.id_item == projetItemDto.id_item))
        {
            throw new InvalidOperationException($"ProjectItem with id_project '{projetItemDto.id_project}' and id_item '{projetItemDto.id_item}' already exists");
        }
        var newProjetItem = _mapper.Map<ProjectsItems>(projetItemDto);
        _context.ProjectsItems.Add(newProjetItem);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectItemDto>(newProjetItem);
    }

    public async Task<ReadBulkProjectItemDto> CreateBulkProjetItem(List<CreateProjectItemDto> projetItemBulkDto)
    {
        var validQuery = new List<ReadProjectItemDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projetItemDto in projetItemBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjetItem(projetItemDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projetItemDto
                });
            }
        }
        return new ReadBulkProjectItemDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadProjectItemDto> UpdateProjetItem(int projetId, int itemId, UpdateProjectItemDto projetItemDto)
    {
        // check if the project exists
        if (!await _context.Projects.AnyAsync(p => p.id_project == projetId))
        {
            throw new KeyNotFoundException($"Project with id '{projetId}' not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var projetItemToUpdate = await _context.ProjectsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjectItem with id_project '{projetId}' and id_item '{itemId}' not found");
        if (projetItemDto.quantity_project_item is not null)
        {
            projetItemToUpdate.quantity_project_item = projetItemDto.quantity_project_item.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjectItemDto>(projetItemToUpdate);
    }

    public async Task DeleteProjetItem(int projetId, int itemId)
    {
        var projetItemToDelete = await _context.ProjectsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjectItem with id_project '{projetId}' and id_item '{itemId}' not found");
        _context.ProjectsItems.Remove(projetItemToDelete);
        await _context.SaveChangesAsync();
    }
}