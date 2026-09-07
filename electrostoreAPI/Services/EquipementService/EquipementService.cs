using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.EquipementStatusService;
using ElectrostoreAPI.Services.FileService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.EquipementService;

public class EquipementService : IEquipementService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly IEquipementStatusService _equipementStatusService;
    private readonly string _equipementDocumentsPath = "equipementDocuments";

    public EquipementService(IMapper mapper, ApplicationDbContext context, IFileService fileService, IEquipementStatusService equipementStatusService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
        _equipementStatusService = equipementStatusService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementDto>> GetEquipements(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Equipements.AsQueryable();
        var filterResult = default(Expression<Func<Equipements, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(e => idResearch.Contains(e.id_equipement));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Equipements>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Equipements>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { field = "id_equipement", order = "asc" };
                    query = query.OrderBy(e => e.id_equipement);
                }
            }
            else
            {
                query = query.OrderBy(e => e.id_equipement);
            }
        }
        query = query.Skip(offset).Take(limit);
        var equipement = await query
            .Select(e => new
            {
                Equipement = e,
                EquipementsTagsCount = e.EquipementsTags.Count,
                EquipementsBoxsCount = e.EquipementsBoxs.Count,
                EquipementsDocumentsCount = e.EquipementsDocuments.Count,
                EquipementsMaintenancesCount = e.EquipementsMaintenances.Count,
                EquipementsStatusCount = e.EquipementsStatus.Count,
                EquipementsCommentsCount = e.EquipementsComments.Count,
                EquipementsTags = expand != null && expand.Contains("equipement_tags") ? e.EquipementsTags.Take(20).ToList() : null,
                EquipementsBoxs = expand != null && expand.Contains("equipement_boxs") ? e.EquipementsBoxs.Take(20).ToList() : null,
                EquipementsDocuments = expand != null && expand.Contains("equipement_documents") ? e.EquipementsDocuments.Take(20).ToList() : null,
                EquipementsMaintenances = expand != null && expand.Contains("equipement_maintenances") ? e.EquipementsMaintenances.Take(20).ToList() : null,
                EquipementsStatusHistory = expand != null && expand.Contains("equipement_status_history") ? e.EquipementsStatus.OrderByDescending(s => s.created_at).Take(20).ToList() : null,
                EquipementsComments = expand != null && expand.Contains("equipement_comments") ? e.EquipementsComments.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementDto>
        {
            data = equipement.Select(e => {
                return _mapper.Map<ReadExtendedEquipementDto>(e.Equipement) with
                {
                    equipement_tags_count = e.EquipementsTagsCount,
                    equipement_boxs_count = e.EquipementsBoxsCount,
                    equipement_documents_count = e.EquipementsDocumentsCount,
                    equipement_maintenances_count = e.EquipementsMaintenancesCount,
                    equipement_status_history_count = e.EquipementsStatusCount,
                    equipement_comments_count = e.EquipementsCommentsCount,
                    equipement_tags = _mapper.Map<IEnumerable<ReadEquipementTagDto>>(e.EquipementsTags),
                    equipement_boxs = _mapper.Map<IEnumerable<ReadEquipementBoxDto>>(e.EquipementsBoxs),
                    equipement_documents = _mapper.Map<IEnumerable<ReadEquipementDocumentDto>>(e.EquipementsDocuments),
                    equipement_maintenances = _mapper.Map<IEnumerable<ReadEquipementMaintenanceDto>>(e.EquipementsMaintenances),
                    equipement_status_history = _mapper.Map<IEnumerable<ReadEquipementStatusDto>>(e.EquipementsStatusHistory),
                    equipement_comments = _mapper.Map<IEnumerable<ReadEquipementCommentDto>>(e.EquipementsComments)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Equipements.CountAsync(filterResult ?? (e => true)),
                next_offset = offset + limit,
                has_more = await _context.Equipements.Skip(offset + limit).AnyAsync(filterResult ?? (e => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedEquipementDto> GetEquipementById(int id, List<string>? expand = null)
    {
        var query = _context.Equipements.AsQueryable();
        query = query.Where(e => e.id_equipement == id);
        var equipement = await query
            .Select(e => new
            {
                Equipement = e,
                EquipementsTagsCount = e.EquipementsTags.Count,
                EquipementsBoxsCount = e.EquipementsBoxs.Count,
                EquipementsDocumentsCount = e.EquipementsDocuments.Count,
                EquipementsMaintenancesCount = e.EquipementsMaintenances.Count,
                EquipementsStatusCount = e.EquipementsStatus.Count,
                EquipementsCommentsCount = e.EquipementsComments.Count,
                EquipementsTags = expand != null && expand.Contains("equipement_tags") ? e.EquipementsTags.Take(20).ToList() : null,
                EquipementsBoxs = expand != null && expand.Contains("equipement_boxs") ? e.EquipementsBoxs.Take(20).ToList() : null,
                EquipementsDocuments = expand != null && expand.Contains("equipement_documents") ? e.EquipementsDocuments.Take(20).ToList() : null,
                EquipementsMaintenances = expand != null && expand.Contains("equipement_maintenances") ? e.EquipementsMaintenances.Take(20).ToList() : null,
                EquipementsStatusHistory = expand != null && expand.Contains("equipement_status_history") ? e.EquipementsStatus.OrderByDescending(s => s.created_at).Take(20).ToList() : null,
                EquipementsComments = expand != null && expand.Contains("equipement_comments") ? e.EquipementsComments.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Equipement with id '{id}' not found");
        return _mapper.Map<ReadExtendedEquipementDto>(equipement.Equipement) with
        {
            equipement_tags_count = equipement.EquipementsTagsCount,
            equipement_boxs_count = equipement.EquipementsBoxsCount,
            equipement_documents_count = equipement.EquipementsDocumentsCount,
            equipement_maintenances_count = equipement.EquipementsMaintenancesCount,
            equipement_status_history_count = equipement.EquipementsStatusCount,
            equipement_comments_count = equipement.EquipementsCommentsCount,
            equipement_tags = _mapper.Map<IEnumerable<ReadEquipementTagDto>>(equipement.EquipementsTags),
            equipement_boxs = _mapper.Map<IEnumerable<ReadEquipementBoxDto>>(equipement.EquipementsBoxs),
            equipement_documents = _mapper.Map<IEnumerable<ReadEquipementDocumentDto>>(equipement.EquipementsDocuments),
            equipement_maintenances = _mapper.Map<IEnumerable<ReadEquipementMaintenanceDto>>(equipement.EquipementsMaintenances),
            equipement_status_history = _mapper.Map<IEnumerable<ReadEquipementStatusDto>>(equipement.EquipementsStatusHistory),
            equipement_comments = _mapper.Map<IEnumerable<ReadEquipementCommentDto>>(equipement.EquipementsComments)
        };
    }

    public async Task<ReadEquipementDto> CreateEquipement(CreateEquipementDto equipementDto)
    {
        // check if equipement already exists
        if (await _context.Equipements.AnyAsync(e => e.reference_name_equipement == equipementDto.reference_name_equipement))
        {
            throw new InvalidOperationException($"Equipement with name '{equipementDto.reference_name_equipement}' already exists");
        }
        var equipement = _mapper.Map<Equipements>(equipementDto);
        _context.Equipements.Add(equipement);
        await _fileService.CreateDirectory(Path.Combine(_equipementDocumentsPath, equipement.id_equipement.ToString()));
        await _context.SaveChangesAsync();
        await _equipementStatusService.CreateEquipementStatus(new CreateEquipementStatusDto
        {
            id_equipement = equipement.id_equipement,
            status_equipement = equipement.status_equipement
        });
        return _mapper.Map<ReadEquipementDto>(equipement);
    }

    public async Task<ReadEquipementDto> UpdateEquipement(int id, UpdateEquipementDto equipementDto)
    {
        var equipementToUpdate = await _context.Equipements.FindAsync(id) ?? throw new KeyNotFoundException($"Equipement with id '{id}' not found");
        var statusChanged = equipementDto.status_equipement.HasValue && equipementDto.status_equipement.Value != equipementToUpdate.status_equipement;
        if (equipementDto.reference_name_equipement is not null)
        {
            // check if another equipement with the name already exists
            if (await _context.Equipements.AnyAsync(e => e.reference_name_equipement == equipementDto.reference_name_equipement && e.id_equipement != id))
            {
                throw new InvalidOperationException($"Equipement with name '{equipementDto.reference_name_equipement}' already exists");
            }
            equipementToUpdate.reference_name_equipement = equipementDto.reference_name_equipement;
        }
        if (equipementDto.friendly_name_equipement is not null)
        {
            equipementToUpdate.friendly_name_equipement = equipementDto.friendly_name_equipement;
        }
        if (equipementDto.description_equipement is not null)
        {
            equipementToUpdate.description_equipement = equipementDto.description_equipement;
        }
        if (equipementDto.status_equipement is not null)
        {
            equipementToUpdate.status_equipement = equipementDto.status_equipement.Value;
        }
        await _context.SaveChangesAsync();
        if (statusChanged)
        {
            await _equipementStatusService.CreateEquipementStatus(new CreateEquipementStatusDto
            {
                id_equipement = equipementToUpdate.id_equipement,
                status_equipement = equipementToUpdate.status_equipement
            });
        }
        return _mapper.Map<ReadEquipementDto>(equipementToUpdate);
    }

    public async Task DeleteEquipement(int id)
    {
        var equipementToDelete = await _context.Equipements.FindAsync(id) ?? throw new KeyNotFoundException($"Equipement with id '{id}' not found");
        _context.Equipements.Remove(equipementToDelete);
        await _fileService.DeleteDirectory(Path.Combine(_equipementDocumentsPath, id.ToString()));
        await _context.SaveChangesAsync();
    }
}
