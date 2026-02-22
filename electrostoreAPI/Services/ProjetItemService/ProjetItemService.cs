using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Extensions;
using electrostore.Models;

namespace electrostore.Services.ProjetItemService;

public class ProjetItemService : IProjetItemService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjetItemService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjetItemDto>> GetProjetItemsByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id '{projetId}' not found");
        }
        var query = _context.ProjetsItems.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_projet", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            var filterResult = RsqlParserExtensions.ToFilterExpression<ProjetsItems>(rsql);
            query = query.Where(filterResult.Item1);
            rsql = filterResult.Item2;
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjetsItems>(sort);
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
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(pi => pi.Projet);
        }
        var projetItem = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjetItemDto>
        {
            data = _mapper.Map<List<ReadExtendedProjetItemDto>>(projetItem),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjetsItems.CountAsync(pi => pi.id_projet == projetId),
                nextOffset = offset + limit,
                hasMore = await _context.ProjetsItems.Skip(offset + limit).AnyAsync(pi => pi.id_projet == projetId)
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjetItemDto>> GetProjetItemsByItemId(int itemId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var query = _context.ProjetsItems.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_item", SearchType = "eq", Value = itemId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            var filterResult = RsqlParserExtensions.ToFilterExpression<ProjetsItems>(rsql);
            query = query.Where(filterResult.Item1);
            rsql = filterResult.Item2;
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjetsItems>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_projet", Order = "asc" };
                query = query.OrderBy(pi => pi.id_projet);
            }
        }
        else
        {
            query = query.OrderBy(pi => pi.id_projet);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(pi => pi.Item);
        }
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(pi => pi.Projet);
        }
        var projetItem = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjetItemDto>
        {
            data = _mapper.Map<List<ReadExtendedProjetItemDto>>(projetItem),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjetsItems.CountAsync(pi => pi.id_item == itemId),
                nextOffset = offset + limit,
                hasMore = await _context.ProjetsItems.Skip(offset + limit).AnyAsync(pi => pi.id_item == itemId)
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjetItemDto> GetProjetItemById(int projetId, int itemId, List<string>? expand = null)
    {
        var query = _context.ProjetsItems.AsQueryable();
        query = query.Where(pi => pi.id_projet == projetId && pi.id_item == itemId);
        if (expand != null && expand.Contains("item"))
        {
            query = query.Include(pi => pi.Item);
        }
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(pi => pi.Projet);
        }
        var projetItem = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjetItem with id_projet '{projetId}' and id_item '{itemId}' not found");
        return _mapper.Map<ReadExtendedProjetItemDto>(projetItem);
    }

    public async Task<ReadProjetItemDto> CreateProjetItem(CreateProjetItemDto projetItemDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetItemDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id '{projetItemDto.id_projet}' not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == projetItemDto.id_item))
        {
            throw new KeyNotFoundException($"Item with id '{projetItemDto.id_item}' not found");
        }
        // check if the projetItem already exists
        if (await _context.ProjetsItems.AnyAsync(pi => pi.id_projet == projetItemDto.id_projet && pi.id_item == projetItemDto.id_item))
        {
            throw new InvalidOperationException($"ProjetItem with id_projet '{projetItemDto.id_projet}' and id_item '{projetItemDto.id_item}' already exists");
        }
        var newProjetItem = _mapper.Map<ProjetsItems>(projetItemDto);
        _context.ProjetsItems.Add(newProjetItem);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetItemDto>(newProjetItem);
    }

    public async Task<ReadBulkProjetItemDto> CreateBulkProjetItem(List<CreateProjetItemDto> projetItemBulkDto)
    {
        var validQuery = new List<ReadProjetItemDto>();
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
        return new ReadBulkProjetItemDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadProjetItemDto> UpdateProjetItem(int projetId, int itemId, UpdateProjetItemDto projetItemDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id '{projetId}' not found");
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id '{itemId}' not found");
        }
        var projetItemToUpdate = await _context.ProjetsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjetItem with id_projet '{projetId}' and id_item '{itemId}' not found");
        if (projetItemDto.qte_projet_item is not null)
        {
            projetItemToUpdate.qte_projet_item = projetItemDto.qte_projet_item.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetItemDto>(projetItemToUpdate);
    }

    public async Task DeleteProjetItem(int projetId, int itemId)
    {
        var projetItemToDelete = await _context.ProjetsItems.FindAsync(projetId, itemId) ?? throw new KeyNotFoundException($"ProjetItem with id_projet '{projetId}' and id_item '{itemId}' not found");
        _context.ProjetsItems.Remove(projetItemToDelete);
        await _context.SaveChangesAsync();
    }
}