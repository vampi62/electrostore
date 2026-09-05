using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.EquipementMaintenanceService;

public class EquipementMaintenanceService : IEquipementMaintenanceService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public EquipementMaintenanceService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementMaintenanceDto>> GetEquipementsMaintenancesByEquipementId(int equipementId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementId))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementId}' not found");
        }
        var query = _context.EquipementsMaintenances.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsMaintenances, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { field = "id_equipement", search_type = "eq", value = equipementId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsMaintenances>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsMaintenances>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { field = "date_planned_equipement_maintenance", order = "asc" };
                query = query.OrderBy(em => em.date_planned_equipement_maintenance);
            }
        }
        else
        {
            query = query.OrderBy(em => em.date_planned_equipement_maintenance);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(em => em.Equipement);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(em => em.User);
        }
        var equipementMaintenance = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementMaintenanceDto>
        {
            data = _mapper.Map<List<ReadExtendedEquipementMaintenanceDto>>(equipementMaintenance),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsMaintenances.CountAsync(filterResult ?? (em => em.id_equipement == equipementId)),
                next_offset = offset + limit,
                has_more = await _context.EquipementsMaintenances.Skip(offset + limit).AnyAsync(filterResult ?? (em => em.id_equipement == equipementId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedEquipementMaintenanceDto> GetEquipementMaintenanceById(int id, int? equipementId = null, List<string>? expand = null)
    {
        var query = _context.EquipementsMaintenances.AsQueryable();
        query = query.Where(em => em.id_equipement_maintenance == id && (equipementId == null || em.id_equipement == equipementId));
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(em => em.Equipement);
        }
        if (expand != null && expand.Contains("user"))
        {
            query = query.Include(em => em.User);
        }
        var equipementMaintenance = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"EquipementMaintenance with id '{id}' not found");
        return _mapper.Map<ReadExtendedEquipementMaintenanceDto>(equipementMaintenance);
    }

    public async Task<ReadEquipementMaintenanceDto> CreateEquipementMaintenance(CreateEquipementMaintenanceDto equipementMaintenanceDto)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementMaintenanceDto.id_equipement))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementMaintenanceDto.id_equipement}' not found");
        }
        // check if the user exists
        if (equipementMaintenanceDto.id_user is not null && !await _context.Users.AnyAsync(u => u.id_user == equipementMaintenanceDto.id_user))
        {
            throw new KeyNotFoundException($"User with id '{equipementMaintenanceDto.id_user}' not found");
        }
        var newEquipementMaintenance = _mapper.Map<EquipementsMaintenances>(equipementMaintenanceDto);
        _context.EquipementsMaintenances.Add(newEquipementMaintenance);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementMaintenanceDto>(newEquipementMaintenance);
    }

    public async Task<ReadEquipementMaintenanceDto> UpdateEquipementMaintenance(int id, UpdateEquipementMaintenanceDto equipementMaintenanceDto, int? equipementId = null)
    {
        var equipementMaintenanceToUpdate = await _context.EquipementsMaintenances.FindAsync(id) ?? throw new KeyNotFoundException($"EquipementMaintenance with id '{id}' not found");
        if (equipementId is not null && equipementMaintenanceToUpdate.id_equipement != equipementId)
        {
            throw new KeyNotFoundException($"EquipementMaintenance with id '{id}' not found for equipement with id '{equipementId}'");
        }
        if (equipementMaintenanceDto.id_user is not null)
        {
            if (!await _context.Users.AnyAsync(u => u.id_user == equipementMaintenanceDto.id_user))
            {
                throw new KeyNotFoundException($"User with id '{equipementMaintenanceDto.id_user}' not found");
            }
            equipementMaintenanceToUpdate.id_user = equipementMaintenanceDto.id_user;
        }
        if (equipementMaintenanceDto.type_equipement_maintenance is not null)
        {
            equipementMaintenanceToUpdate.type_equipement_maintenance = equipementMaintenanceDto.type_equipement_maintenance.Value;
        }
        if (equipementMaintenanceDto.date_planned_equipement_maintenance is not null)
        {
            equipementMaintenanceToUpdate.date_planned_equipement_maintenance = equipementMaintenanceDto.date_planned_equipement_maintenance.Value;
        }
        if (equipementMaintenanceDto.date_done_equipement_maintenance is not null)
        {
            equipementMaintenanceToUpdate.date_done_equipement_maintenance = equipementMaintenanceDto.date_done_equipement_maintenance;
        }
        if (equipementMaintenanceDto.description_equipement_maintenance is not null)
        {
            equipementMaintenanceToUpdate.description_equipement_maintenance = equipementMaintenanceDto.description_equipement_maintenance;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementMaintenanceDto>(equipementMaintenanceToUpdate);
    }

    public async Task DeleteEquipementMaintenance(int id, int? equipementId = null)
    {
        var equipementMaintenanceToDelete = await _context.EquipementsMaintenances.FindAsync(id) ?? throw new KeyNotFoundException($"EquipementMaintenance with id '{id}' not found");
        if (equipementId is not null && equipementMaintenanceToDelete.id_equipement != equipementId)
        {
            throw new KeyNotFoundException($"EquipementMaintenance with id '{id}' not found for equipement with id '{equipementId}'");
        }
        _context.EquipementsMaintenances.Remove(equipementMaintenanceToDelete);
        await _context.SaveChangesAsync();
    }
}
