using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.EquipementTagService;

public class EquipementTagService : IEquipementTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public EquipementTagService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementTagDto>> GetEquipementsTagsByEquipementId(int equipementId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if the equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementId))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementId}' not found");
        }
        var query = _context.EquipementsTags.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_equipement", SearchType = "eq", Value = equipementId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_tag", Order = "asc" };
                query = query.OrderBy(et => et.id_tag);
            }
        }
        else
        {
            query = query.OrderBy(et => et.id_tag);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(et => et.Tag);
        }
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(et => et.Equipement);
        }
        var equipementTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementTagDto>
        {
            data = _mapper.Map<List<ReadExtendedEquipementTagDto>>(equipementTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsTags.CountAsync(filterResult ?? (et => et.id_equipement == equipementId)),
                nextOffset = offset + limit,
                hasMore = await _context.EquipementsTags.Skip(offset + limit).AnyAsync(filterResult ?? (et => et.id_equipement == equipementId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedEquipementTagDto>> GetEquipementsTagsByTagId(int tagId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id '{tagId}' not found");
        }
        var query = _context.EquipementsTags.AsQueryable();
        var filterResult = default(Expression<Func<EquipementsTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_tag", SearchType = "eq", Value = tagId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<EquipementsTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<EquipementsTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_equipement", Order = "asc" };
                query = query.OrderBy(et => et.id_equipement);
            }
        }
        else
        {
            query = query.OrderBy(et => et.id_equipement);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(et => et.Tag);
        }
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(et => et.Equipement);
        }
        var equipementTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedEquipementTagDto>
        {
            data = _mapper.Map<List<ReadExtendedEquipementTagDto>>(equipementTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.EquipementsTags.CountAsync(filterResult ?? (et => et.id_tag == tagId)),
                nextOffset = offset + limit,
                hasMore = await _context.EquipementsTags.Skip(offset + limit).AnyAsync(filterResult ?? (et => et.id_tag == tagId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedEquipementTagDto> GetEquipementTagById(int equipementId, int tagId, List<string>? expand = null)
    {
        var query = _context.EquipementsTags.AsQueryable();
        query = query.Where(et => et.id_equipement == equipementId && et.id_tag == tagId);
        if (expand != null && expand.Contains("tag"))
        {
            query = query.Include(et => et.Tag);
        }
        if (expand != null && expand.Contains("equipement"))
        {
            query = query.Include(et => et.Equipement);
        }
        var equipementTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"EquipementTag with id_equipement '{equipementId}' and id_tag '{tagId}' not found");
        return _mapper.Map<ReadExtendedEquipementTagDto>(equipementTag);
    }

    public async Task<ReadEquipementTagDto> CreateEquipementTag(CreateEquipementTagDto equipementTagDto)
    {
        // check if equipement exists
        if (!await _context.Equipements.AnyAsync(e => e.id_equipement == equipementTagDto.id_equipement))
        {
            throw new KeyNotFoundException($"Equipement with id '{equipementTagDto.id_equipement}' not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(t => t.id_tag == equipementTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id '{equipementTagDto.id_tag}' not found");
        }
        // check if equipementTag already exists
        if (await _context.EquipementsTags.AnyAsync(et => et.id_equipement == equipementTagDto.id_equipement && et.id_tag == equipementTagDto.id_tag))
        {
            throw new InvalidOperationException($"EquipementTag with id_equipement '{equipementTagDto.id_equipement}' and id_tag '{equipementTagDto.id_tag}' already exists");
        }
        var equipementTag = _mapper.Map<EquipementsTags>(equipementTagDto);
        _context.EquipementsTags.Add(equipementTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadEquipementTagDto>(equipementTag);
    }

    public async Task<ReadBulkEquipementTagDto> CreateBulkEquipementTag(List<CreateEquipementTagDto> equipementTagBulkDto)
    {
        var validQuery = new List<ReadEquipementTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var equipementTagDto in equipementTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateEquipementTag(equipementTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = equipementTagDto
                });
            }
        }
        return new ReadBulkEquipementTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteEquipementTag(int equipementId, int tagId)
    {
        var equipementTagToDelete = await _context.EquipementsTags.FindAsync(equipementId, tagId) ?? throw new KeyNotFoundException($"EquipementTag with id_equipement '{equipementId}' and id_tag '{tagId}' not found");
        _context.EquipementsTags.Remove(equipementTagToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkEquipementTagDto> DeleteBulkEquipementTag(List<CreateEquipementTagDto> equipementTagBulkDto)
    {
        var validQuery = new List<ReadEquipementTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var equipementTagDto in equipementTagBulkDto)
        {
            try
            {
                await DeleteEquipementTag(equipementTagDto.id_equipement, equipementTagDto.id_tag);
                validQuery.Add(new ReadEquipementTagDto
                {
                    id_equipement = equipementTagDto.id_equipement,
                    id_tag = equipementTagDto.id_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = equipementTagDto
                });
            }
        }
        return new ReadBulkEquipementTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}
