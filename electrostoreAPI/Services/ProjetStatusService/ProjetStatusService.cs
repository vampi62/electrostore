using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Enums;
using electrostore.Extensions;
using electrostore.Models;

namespace electrostore.Services.ProjetStatusService;

public class ProjetStatusService : IProjetStatusService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjetStatusService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjetStatusDto>> GetProjetStatusByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id '{projetId}' not found");
        }
        var query = _context.ProjetsStatus.AsQueryable();
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_projet", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            var filterResult = RsqlParserExtensions.ToFilterExpression<ProjetsStatus>(rsql);
            query = query.Where(filterResult.Item1);
            rsql = filterResult.Item2;
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjetsStatus>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "created_at", Order = "desc" };
                query = query.OrderByDescending(p => p.created_at);
            }
        }
        else
        {
            query = query.OrderByDescending(p => p.created_at);
        }
        query = query.Skip(offset).Take(limit);
        var projetStatus = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjetStatusDto>
        {
            data = _mapper.Map<List<ReadExtendedProjetStatusDto>>(projetStatus),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjetsStatus.CountAsync(p => p.id_projet == projetId),
                nextOffset = offset + limit,
                hasMore = await _context.ProjetsStatus.Skip(offset + limit).AnyAsync(p => p.id_projet == projetId)
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjetStatusDto> GetProjetStatusById(int id, int? projetId = null)
    {
        var query = _context.ProjetsStatus.AsQueryable();
        query = query.Where(pc => pc.id_projet_status == id && (projetId == null || pc.id_projet == projetId));
        var projetStatus = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjetStatus with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjetStatusDto>(projetStatus);
    }

    public async Task<ReadProjetStatusDto> CreateProjetStatus(CreateProjetStatusDto projetStatusDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetStatusDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id '{projetStatusDto.id_projet}' not found");
        }
        var newProjetStatus = _mapper.Map<ProjetsStatus>(projetStatusDto);
        _context.ProjetsStatus.Add(newProjetStatus);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetStatusDto>(newProjetStatus);
    }
}