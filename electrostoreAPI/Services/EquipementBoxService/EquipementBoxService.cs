using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.EquipementBoxService;

public class EquipementBoxService : IEquipementBoxService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public EquipementBoxService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementBoxDto>> GetEquipementsBoxsByBoxId(int boxId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id '{boxId}' not found");
        }
        var query = _context.EquipementsBoxs.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsBoxs, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_box", search_type = "eq", value = boxId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsBoxs>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsBoxs>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_equipement", order = "asc" };
                query = query.OrderBy(eb => eb.id_equipement);
            }
        }
        else
        {
            query = query.OrderBy(eb => eb.id_equipement);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(eb => eb.Equipement);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(eb => eb.Box);
        }
        var equipementBox = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementBoxDto>
        {
            data = _mapper.Map<List<ReadExtendedEquipementBoxDto>>(equipementBox),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsBoxs.CountAsync(filterResult ?? (eb => eb.id_box == boxId)),
                next_offset = offset + limit,
                has_more = await _context.EquipementsBoxs.Skip(offset + limit).AnyAsync(filterResult ?? (eb => eb.id_box == boxId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementBoxDto>> GetEquipementsBoxsByEquipementId(int equipementId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementId))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementId}' not found");
        }
        var query = _context.EquipementsBoxs.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsBoxs, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_equipement", search_type = "eq", value = equipementId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsBoxs>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsBoxs>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "id_box", order = "asc" };
                query = query.OrderBy(eb => eb.id_box);
            }
        }
        else
        {
            query = query.OrderBy(eb => eb.id_box);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(eb => eb.Equipement);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(eb => eb.Box);
        }
        var equipementBox = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementBoxDto>
        {
            data = _mapper.Map<List<ReadExtendedEquipementBoxDto>>(equipementBox),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsBoxs.CountAsync(filterResult ?? (eb => eb.id_equipement == equipementId)),
                next_offset = offset + limit,
                has_more = await _context.EquipementsBoxs.Skip(offset + limit).AnyAsync(filterResult ?? (eb => eb.id_equipement == equipementId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedEquipementBoxDto> GetEquipementBoxById(int equipementId, int boxId, List<string>? expand = null)
    {
        var query = _context.EquipementsBoxs.AsQueryable();
        query = query.Where(eb => eb.id_box == boxId && eb.id_equipement == equipementId);
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(eb => eb.Equipement);
        }
        if (expand != null && expand.Contains("box"))
        {
            query = query.Include(eb => eb.Box);
        }
        var equipementBox = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"EquipementBox with id '{equipementId}' and boxId '{boxId}' not found");
        return _mapper.Map<ReadExtendedEquipementBoxDto>(equipementBox);
    }

    public async Task<ReadEquipementBoxDto> CreateEquipementBox(CreateEquipementBoxDto equipementBoxDto)
    {
        // check if the box exists
        if (!await _context.Boxs.AnyAsync(b => b.id_box == equipementBoxDto.id_box))
        {
            throw new KeyNotFoundException($"Box with id '{equipementBoxDto.id_box}' not found");
        }
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementBoxDto.id_equipement))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementBoxDto.id_equipement}' not found");
        }
        // check if the equipement is already assigned to a box
        if (await _context.EquipementsBoxs.AnyAsync(eb => eb.id_equipement == equipementBoxDto.id_equipement))
        {
            throw new InvalidOperationException("Equipement is already assigned to a box, remove it from its current box first");
        }
        var newEquipementBox = _mapper.Map<EquipementsBoxs>(equipementBoxDto);
        _context.EquipementsBoxs.Add(newEquipementBox);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementBoxDto>(newEquipementBox);
    }

    public async Task DeleteEquipementBox(int equipementId, int boxId)
    {
        var equipementBoxToDelete = await _context.EquipementsBoxs.FindAsync(equipementId, boxId) ?? throw new KeyNotFoundException($"EquipementBox with id '{equipementId}' and boxId '{boxId}' not found");
        _context.EquipementsBoxs.Remove(equipementBoxToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task CheckIfStoreExists(int storeId, int boxId)
    {
        if (!await _context.Boxs.AnyAsync(b => b.id_box == boxId && b.id_store == storeId))
        {
            throw new KeyNotFoundException($"Box with id '{boxId}' not found in store with id '{storeId}'");
        }
    }
}
