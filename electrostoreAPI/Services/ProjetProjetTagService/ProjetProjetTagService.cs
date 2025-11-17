using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Enums;
using electrostore.Services.SessionService;

namespace electrostore.Services.ProjetProjetTagService;

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

    public async Task<IEnumerable<ReadExtendedProjetProjetTagDto>> GetProjetsProjetTagsByProjetId(int projetId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if projet exists
        if (!await _context.Projets.AnyAsync(s => s.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id '{projetId}' not found");
        }
        var query = _context.ProjetsProjetTags.AsQueryable();
        query = query.Where(st => st.id_projet == projetId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(st => st.id_projet_tag);
        if (expand != null && expand.Contains("projet"))
        {
            query = query.Include(st => st.Projet);
        }
        if (expand != null && expand.Contains("projet_tag"))
        {
            query = query.Include(st => st.ProjetTag);
        }
        var projetProjetTag = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedProjetProjetTagDto>>(projetProjetTag);
    }

    public async Task<int> GetProjetsProjetTagsCountByProjetId(int projetId)
    {
        // check if projet exists
        if (!await _context.Projets.AnyAsync(s => s.id_projet == projetId))
        {
            throw new KeyNotFoundException($"Projet with id '{projetId}' not found");
        }
        return await _context.ProjetsProjetTags
            .CountAsync(st => st.id_projet == projetId);
    }

    public async Task<IEnumerable<ReadExtendedProjetProjetTagDto>> GetProjetsProjetTagsByprojetTagId(int projetTagId, int limit = 100, int offset = 0, List<string>? expand = null)
    {
        // check if projetTag exists
        if (!await _context.ProjetTags.AnyAsync(t => t.id_projet_tag == projetTagId))
        {
            throw new KeyNotFoundException($"ProjetTag with id '{projetTagId}' not found");
        }
        var query = _context.ProjetsProjetTags.AsQueryable();
        query = query.Where(st => st.id_projet_tag == projetTagId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(st => st.id_projet);
        if (expand != null && expand.Contains("projet_tag"))
        {
            query = query.Include(st => st.ProjetTag);
        }
        if (expand != null && expand.Contains("Projet"))
        {
            query = query.Include(st => st.Projet);
        }
        var projetProjetTag = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedProjetProjetTagDto>>(projetProjetTag);
    }

    public async Task<int> GetProjetsProjetTagsCountByprojetTagId(int projetTagId)
    {
        // check if projetTag exists
        if (!await _context.ProjetTags.AnyAsync(t => t.id_projet_tag == projetTagId))
        {
            throw new KeyNotFoundException($"ProjetTag with id '{projetTagId}' not found");
        }
        return await _context.ProjetsProjetTags
            .CountAsync(st => st.id_projet_tag == projetTagId);
    }

    public async Task<ReadExtendedProjetProjetTagDto> GetProjetProjetTagById(int projetId, int projetTagId, List<string>? expand = null)
    {
        var query = _context.ProjetsProjetTags.AsQueryable();
        query = query.Where(st => st.id_projet == projetId && st.id_projet_tag == projetTagId);
        if (expand != null && expand.Contains("projet_tag"))
        {
            query = query.Include(st => st.ProjetTag);
        }
        if (expand != null && expand.Contains("Projet"))
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
        if (!await _context.Projets.AnyAsync(s => s.id_projet == projetProjetTagDto.id_projet))
        {
            throw new KeyNotFoundException($"Projet with id '{projetProjetTagDto.id_projet}' not found");
        }
        // check if tag exists
        if (!await _context.ProjetTags.AnyAsync(t => t.id_projet_tag == projetProjetTagDto.id_projet_tag))
        {
            throw new KeyNotFoundException($"Tag with id '{projetProjetTagDto.id_projet_tag}' not found");
        }
        // check if store tag already exists
        if (await _context.ProjetsProjetTags.AnyAsync(st => st.id_projet == projetProjetTagDto.id_projet && st.id_projet_tag == projetProjetTagDto.id_projet_tag))
        {
            throw new InvalidOperationException($"ProjetProjetTag with projetId '{projetProjetTagDto.id_projet}' and projetTagId '{projetProjetTagDto.id_projet_tag}' already exists");
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
                await DeleteProjetProjetTag(projetProjetTagDto.id_projet, projetProjetTagDto.id_projet_tag);
                validQuery.Add(new ReadProjetProjetTagDto
                {
                    id_projet = projetProjetTagDto.id_projet,
                    id_projet_tag = projetProjetTagDto.id_projet_tag
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