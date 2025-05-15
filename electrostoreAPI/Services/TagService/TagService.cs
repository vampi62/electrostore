using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.TagService;

public class TagService : ITagService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public TagService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedTagDto>> GetTags(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Tags.AsQueryable();
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(b => idResearch.Contains(b.id_tag));
        }
        query = query.Skip(offset).Take(limit);
        if (expand != null && expand.Contains("stores_tags"))
        {
            query = query.Include(t => t.StoresTags);
        }
        if (expand != null && expand.Contains("items_tags"))
        {
            query = query.Include(t => t.ItemsTags);
        }
        if (expand != null && expand.Contains("boxs_tags"))
        {
            query = query.Include(t => t.BoxsTags);
        }
        var tag = await query.ToListAsync();
        return _mapper.Map<List<ReadExtendedTagDto>>(tag);
    }

    public async Task<int> GetTagsCount()
    {
        return await _context.Tags.CountAsync();
    }

    public async Task<ReadExtendedTagDto> GetTagById(int id, List<string>? expand = null)
    {
        var query = _context.Tags.AsQueryable();
        query = query.Where(t => t.id_tag == id);
        if (expand != null && expand.Contains("stores_tags"))
        {
            query = query.Include(t => t.StoresTags);
        }
        if (expand != null && expand.Contains("items_tags"))
        {
            query = query.Include(t => t.ItemsTags);
        }
        if (expand != null && expand.Contains("boxs_tags"))
        {
            query = query.Include(t => t.BoxsTags);
        }
        var tag = await query.FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Tag with id {id} not found");
        return _mapper.Map<ReadExtendedTagDto>(tag);
    }

    public async Task<ReadTagDto> CreateTag(CreateTagDto tagDto)
    {
        // check if tag name already exists
        if (await _context.Tags.AnyAsync(t => t.nom_tag == tagDto.nom_tag))
        {
            throw new InvalidOperationException($"Tag with name {tagDto.nom_tag} already exists");
        }
        var newTag = new Tags
        {
            nom_tag = tagDto.nom_tag,
            poids_tag = tagDto.poids_tag ?? 0
        };
        _context.Tags.Add(newTag);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadTagDto>(newTag);
    }

    public async Task<ReadBulkTagDto> CreateBulkTag(List<CreateTagDto> tagBulkDto)
    {
        var validQuery = new List<ReadTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var tagDto in tagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateTag(tagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = tagDto
                });
            }
        }
        return new ReadBulkTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task<ReadTagDto> UpdateTag(int id, UpdateTagDto tagDto)
    {
        var tagToUpdate = await _context.Tags.FindAsync(id) ?? throw new KeyNotFoundException($"Tag with id {id} not found");
        if (tagDto.nom_tag is not null)
        {
            // check if tag name already exists
            if (await _context.Tags.AnyAsync(t => t.nom_tag == tagDto.nom_tag))
            {
                throw new InvalidOperationException($"Tag with name {tagDto.nom_tag} already exists");
            }
            tagToUpdate.nom_tag = tagDto.nom_tag;
        }
        if (tagDto.poids_tag is not null)
        {
            tagToUpdate.poids_tag = tagDto.poids_tag.Value;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadTagDto>(tagToUpdate);
    }

    public async Task DeleteTag(int id)
    {
        var tagToDelete = await _context.Tags.FindAsync(id) ?? throw new KeyNotFoundException($"Tag with id {id} not found");
        _context.Tags.Remove(tagToDelete);
        await _context.SaveChangesAsync();
    }
}