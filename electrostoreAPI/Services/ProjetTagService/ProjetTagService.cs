using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Extensions;
using electrostore.Models;
using System.Linq.Expressions;

namespace electrostore.Services.ProjetTagService;

public class ProjetTagService : IProjetTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ProjetTagService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjetTagDto>> GetProjetTags(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.ProjetTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjetTags, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(t => idResearch.Contains(t.id_projet_tag));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjetTags>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<ProjetTags>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_projet_tag", Order = "asc" };
                    query = query.OrderBy(t => t.id_projet_tag);
                }
            }
            else
            {
                query = query.OrderBy(t => t.id_projet_tag);
            }
        }
        query = query.Skip(offset).Take(limit);
        var projetTag = await query
            .Select(t => new
            {
                ProjetTags = t,
                ProjetsProjetTagsCount = t.ProjetsProjetTags.Count,
                ProjetsProjetTags = expand != null && expand.Contains("projets_projet_tags") ? t.ProjetsProjetTags.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjetTagDto>
        {
            data = projetTag.Select(t => _mapper.Map<ReadExtendedProjetTagDto>(t.ProjetTags) with
            {
                projets_projet_tags_count = t.ProjetsProjetTagsCount,
                projets_projet_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(t.ProjetsProjetTags)
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjetTags.CountAsync(filterResult ?? (t => true)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjetTags.Skip(offset + limit).AnyAsync(filterResult ?? (t => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjetTagDto> GetProjetTagById(int id, List<string>? expand = null)
    {
        var query = _context.ProjetTags.AsQueryable();
        query = query.Where(t => t.id_projet_tag == id);
        var projetTag = await query
            .Select(t => new
            {
                ProjetTags = t,
                ProjetsProjetTagsCount = t.ProjetsProjetTags.Count,
                ProjetsProjetTags = expand != null && expand.Contains("projets_projet_tags") ? t.ProjetsProjetTags.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjetTag with id '{id}' not found");
        return _mapper.Map<ReadExtendedProjetTagDto>(projetTag.ProjetTags) with
        {
            projets_projet_tags_count = projetTag.ProjetsProjetTagsCount,
            projets_projet_tags = _mapper.Map<IEnumerable<ReadProjetProjetTagDto>>(projetTag.ProjetsProjetTags)
        };
    }

    public async Task<ReadProjetTagDto> CreateProjetTag(CreateProjetTagDto projetTagDto)
    {
        // check if tag name already exists
        if (await _context.ProjetTags.AnyAsync(t => t.nom_projet_tag == projetTagDto.nom_projet_tag))
        {
            throw new InvalidOperationException($"ProjetTag with name '{projetTagDto.nom_projet_tag}' already exists");
        }
        var newProjetTag = _mapper.Map<ProjetTags>(projetTagDto);
        _context.ProjetTags.Add(newProjetTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetTagDto>(newProjetTag);
    }

    public async Task<ReadBulkProjetTagDto> CreateBulkProjetTag(List<CreateProjetTagDto> projetTagBulkDto)
    {
        var validQuery = new List<ReadProjetTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projetTagDto in projetTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjetTag(projetTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projetTagDto
                });
            }
        }
        return new ReadBulkProjetTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadProjetTagDto> UpdateProjetTag(int id, UpdateProjetTagDto projetTagDto)
    {
        var projetTagToUpdate = await _context.ProjetTags.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetTag with id {id} not found");
        if (projetTagDto.nom_projet_tag is not null)
        {
            // check if another tag with the name already exists
            if (await _context.ProjetTags.AnyAsync(t => t.nom_projet_tag == projetTagDto.nom_projet_tag && t.id_projet_tag != id))
            {
                throw new InvalidOperationException($"ProjetTag with name '{projetTagDto.nom_projet_tag}' already exists");
            }
            projetTagToUpdate.nom_projet_tag = projetTagDto.nom_projet_tag;
        }
        if (projetTagDto.poids_projet_tag is not null)
        {
            projetTagToUpdate.poids_projet_tag = projetTagDto.poids_projet_tag.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetTagDto>(projetTagToUpdate);
    }

    public async Task DeleteProjetTag(int id)
    {
        var projetTagToDelete = await _context.ProjetTags.FindAsync(id) ?? throw new KeyNotFoundException($"ProjetTag with id '{id}' not found");
        _context.ProjetTags.Remove(projetTagToDelete);
        await _context.SaveChangesAsync();
    }
}