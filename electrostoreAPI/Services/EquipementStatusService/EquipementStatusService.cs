using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.EquipementStatusService;

public class EquipementStatusService : IEquipementStatusService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public EquipementStatusService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementStatusDto>> GetEquipementStatusByEquipementId(int equipementId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementId))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementId}' not found");
        }
        var query = _context.EquipementsStatus.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_equipement", SearchType = "eq", Value = equipementId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            var filterResult = RsqlParserExtensions.ToFilterExpression<EquipementsStatus>(rsql);
            query = query.Where(filterResult.Item1);
            rsql = filterResult.Item2;
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsStatus>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(es => es.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(es => es.created_at);
        }
        query = query.Skip(offset).Take(limit);
        var equipementStatus = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementStatusDto>
        {
            data = _mapper.Map<List<ReadExtendedEquipementStatusDto>>(equipementStatus),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsStatus.CountAsync(es => es.id_equipement == equipementId),
                nextOffset = offset + limit,
                hasMore = await _context.EquipementsStatus.Skip(offset + limit).AnyAsync(es => es.id_equipement == equipementId)
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedEquipementStatusDto> GetEquipementStatusById(int id, int? equipementId = null)
    {
        var query = _context.EquipementsStatus.AsQueryable();
        query = query.Where(es => es.id_equipement_status == id && (equipementId == null || es.id_equipement == equipementId));
        var equipementStatus = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"EquipementStatus with id '{id}' not found");
        return _mapper.Map<ReadExtendedEquipementStatusDto>(equipementStatus);
    }

    public async Task<ReadEquipementStatusDto> CreateEquipementStatus(CreateEquipementStatusDto equipementStatusDto)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementStatusDto.id_equipement))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementStatusDto.id_equipement}' not found");
        }
        var newEquipementStatus = _mapper.Map<EquipementsStatus>(equipementStatusDto);
        _context.EquipementsStatus.Add(newEquipementStatus);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementStatusDto>(newEquipementStatus);
    }
}
