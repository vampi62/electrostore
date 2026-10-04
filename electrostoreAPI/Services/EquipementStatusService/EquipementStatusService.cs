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
        rsql.Add(new FilterDto { field = "id_equipement", search_type = "eq", value = equipementId.ToString() });
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "created_at", order = "desc" })
            .ToResponseAsync(equipementStatus => _mapper.Map<List<ReadExtendedEquipementStatusDto>>(equipementStatus));
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
