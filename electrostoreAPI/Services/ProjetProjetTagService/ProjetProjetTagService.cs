using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ProjetProjetTagService;

public class ProjetProjetTagService : IProjetProjetTagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;

    public ProjetProjetTagService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjetProjetTagDto>> GetProjetsProjetTagsByProjetId(int projetId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if projet exists
        if (!await _context.Projets.AnyAsync(s => s.id_project == projetId))
        {
            throw new KeyNotFoundException($"Projet with id '{projetId}' not found");
        }
        var query = _context.ProjetsProjetTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjetsProjetTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project", SearchType = "eq", Value = projetId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjetsProjetTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjetsProjetTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_project_tag", Order = "asc" };
                query = query.OrderBy(st => st.id_project_tag);
            }
        }
        else
        {
            query = query.OrderBy(st => st.id_project_tag);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(st => st.Projet);
        }
        if (expand != null && expand.Contains("projet_tag"))
        {
            query = query.Include(st => st.ProjetTag);
        }
        var projetProjetTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjetProjetTagDto>
        {
            data = _mapper.Map<List<ReadExtendedProjetProjetTagDto>>(projetProjetTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjetsProjetTags.CountAsync(filterResult ?? (st => st.id_project == projetId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjetsProjetTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_project == projetId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<PaginatedResponseDto<ReadExtendedProjetProjetTagDto>> GetProjetsProjetTagsByprojetTagId(int projetTagId, int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        // check if projetTag exists
        if (!await _context.ProjetTags.AnyAsync(t => t.id_project_tag == projetTagId))
        {
            throw new KeyNotFoundException($"ProjetTag with id '{projetTagId}' not found");
        }
        var query = _context.ProjetsProjetTags.AsQueryable();
        var filterResult = default(Expression<Func<ProjetsProjetTags, bool>>);
        rsql ??= [];
        rsql.Add(new FilterDto { Field = "id_project_tag", SearchType = "eq", Value = projetTagId.ToString() });
        if (rsql != null && rsql.Count > 0)
        {
            (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<ProjetsProjetTags>(rsql);
            query = query.Where(filterResult);
        }
        if (!string.IsNullOrEmpty(sort?.Field))
        {
            var sortResult = RsqlParserExtensions.ToSortExpression<ProjetsProjetTags>(sort);
            if (sortResult.Item1 != null)
            {
                query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
            }
            else
            {
                sort = new SorterDto { Field = "id_project", Order = "asc" };
                query = query.OrderBy(st => st.id_project);
            }
        }
        else
        {
            query = query.OrderBy(st => st.id_project);
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("projet_tag"))
        {
            query = query.Include(st => st.ProjetTag);
        }
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(st => st.Projet);
        }
        var projetProjetTag = await query.ToListAsync();
        return new PaginatedResponseDto<ReadExtendedProjetProjetTagDto>
        {
            data = _mapper.Map<List<ReadExtendedProjetProjetTagDto>>(projetProjetTag),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.ProjetsProjetTags.CountAsync(filterResult ?? (st => st.id_project_tag == projetTagId)),
                nextOffset = offset + limit,
                hasMore = await _context.ProjetsProjetTags.Skip(offset + limit).AnyAsync(filterResult ?? (st => st.id_project_tag == projetTagId))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
    }

    public async Task<ReadExtendedProjetProjetTagDto> GetProjetProjetTagById(int projetId, int projetTagId, List<string>? expand = null)
    {
        var query = _context.ProjetsProjetTags.AsQueryable();
        query = query.Where(st => st.id_project == projetId && st.id_project_tag == projetTagId);
        if (expand != null && expand.Contains("projet_tag"))
        {
            query = query.Include(st => st.ProjetTag);
        }
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(st => st.Projet);
        }
        var projetProjetTag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"ProjetProjetTag with projetId '{projetId}' and projetTagId '{projetTagId}' not found");
        return _mapper.Map<ReadExtendedProjetProjetTagDto>(projetProjetTag);
    }

    public async Task<ReadProjetProjetTagDto> CreateProjetProjetTag(CreateProjetProjetTagDto projetProjetTagDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create ProjetProjetTag");
        }
        // check if store exists
        if (!await _context.Projets.AnyAsync(s => s.id_project == projetProjetTagDto.id_project))
        {
            throw new KeyNotFoundException($"Projet with id '{projetProjetTagDto.id_project}' not found");
        }
        // check if tag exists
        if (!await _context.ProjetTags.AnyAsync(t => t.id_project_tag == projetProjetTagDto.id_project_tag))
        {
            throw new KeyNotFoundException($"Tag with id '{projetProjetTagDto.id_project_tag}' not found");
        }
        // check if store tag already exists
        if (await _context.ProjetsProjetTags.AnyAsync(st => st.id_project == projetProjetTagDto.id_project && st.id_project_tag == projetProjetTagDto.id_project_tag))
        {
            throw new InvalidOperationException($"ProjetProjetTag with projetId '{projetProjetTagDto.id_project}' and projetTagId '{projetProjetTagDto.id_project_tag}' already exists");
        }
        var newProjetProjetTag = _mapper.Map<ProjetsProjetTags>(projetProjetTagDto);
        _context.ProjetsProjetTags.Add(newProjetProjetTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadProjetProjetTagDto>(newProjetProjetTag);
    }

    public async Task<ReadBulkProjetProjetTagDto> CreateBulkProjetProjetTag(List<CreateProjetProjetTagDto> projetProjetTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to create ProjetProjetTag");
        }
        var validQuery = new List<ReadProjetProjetTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projetProjetTagDto in projetProjetTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateProjetProjetTag(projetProjetTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projetProjetTagDto
                });
            }
        }
        return new ReadBulkProjetProjetTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteProjetProjetTag(int projetId, int projetTagId)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete ProjetProjetTag");
        }
        var projetProjetTag = await _context.ProjetsProjetTags.FindAsync(projetId, projetTagId) ?? throw new KeyNotFoundException($"ProjetProjetTag with projetId '{projetId}' and projetTagId '{projetTagId}' not found");
        _context.ProjetsProjetTags.Remove(projetProjetTag);
        await _context.SaveChangesAsync();
    }

    public async Task<ReadBulkProjetProjetTagDto> DeleteBulkProjetProjetTag(List<CreateProjetProjetTagDto> projetProjetTagBulkDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You are not authorized to delete ProjetProjetTag");
        }
        var validQuery = new List<ReadProjetProjetTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var projetProjetTagDto in projetProjetTagBulkDto)
        {
            try
            {
                await DeleteProjetProjetTag(projetProjetTagDto.id_project, projetProjetTagDto.id_project_tag);
                validQuery.Add(new ReadProjetProjetTagDto
                {
                    id_project = projetProjetTagDto.id_project,
                    id_project_tag = projetProjetTagDto.id_project_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = projetProjetTagDto
                });
            }
        }
        await _context.SaveChangesAsync();
        return new ReadBulkProjetProjetTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}