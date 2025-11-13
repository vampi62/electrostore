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
            query = query.Where(t => idResearch.Contains(t.id_tag));
        }
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(t => t.id_tag);
        var tags = await query
            .Select(t => new
            {
                Tag = t,
                StoresTagsCount = t.StoresTags.Count,
                ItemsTagsCount = t.ItemsTags.Count,
                BoxsTagsCount = t.BoxsTags.Count,
                StoresTags = expand != null && expand.Contains("stores_tags") ? t.StoresTags.Take(20).ToList() : null,
                ItemsTags = expand != null && expand.Contains("items_tags") ? t.ItemsTags.Take(20).ToList() : null,
                BoxsTags = expand != null && expand.Contains("boxs_tags") ? t.BoxsTags.Take(20).ToList() : null
            })
            .ToListAsync();
        return tags.Select(t => {
            return _mapper.Map<ReadExtendedTagDto>(t.Tag) with
            {
                stores_tags_count = t.StoresTagsCount,
                items_tags_count = t.ItemsTagsCount,
                boxs_tags_count = t.BoxsTagsCount,
                stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(t.StoresTags),
                items_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(t.ItemsTags),
                boxs_tags = _mapper.Map<IEnumerable<ReadBoxTagDto>>(t.BoxsTags)
            };
        }).ToList();
    }

    public async Task<int> GetTagsCount()
    {
        return await _context.Tags.CountAsync();
    }

    public async Task<ReadExtendedTagDto> GetTagById(int id, List<string>? expand = null)
    {
        var query = _context.Tags.AsQueryable();
        query = query.Where(t => t.id_tag == id);
        var tag = await query
            .Select(t => new
            {
                Tag = t,
                StoresTagsCount = t.StoresTags.Count,
                ItemsTagsCount = t.ItemsTags.Count,
                BoxsTagsCount = t.BoxsTags.Count,
                StoresTags = expand != null && expand.Contains("stores_tags") ? t.StoresTags.Take(20).ToList() : null,
                ItemsTags = expand != null && expand.Contains("items_tags") ? t.ItemsTags.Take(20).ToList() : null,
                BoxsTags = expand != null && expand.Contains("boxs_tags") ? t.BoxsTags.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Tag with id {id} not found");
        return _mapper.Map<ReadExtendedTagDto>(tag.Tag) with
        {
            stores_tags_count = tag.StoresTagsCount,
            items_tags_count = tag.ItemsTagsCount,
            boxs_tags_count = tag.BoxsTagsCount,
            stores_tags = _mapper.Map<IEnumerable<ReadStoreTagDto>>(tag.StoresTags),
            items_tags = _mapper.Map<IEnumerable<ReadItemTagDto>>(tag.ItemsTags),
            boxs_tags = _mapper.Map<IEnumerable<ReadBoxTagDto>>(tag.BoxsTags)
        };
    }

    public async Task<ReadTagDto> CreateTag(CreateTagDto tagDto)
    {
        // check if tag name already exists
        if (await _context.Tags.AnyAsync(t => t.nom_tag == tagDto.nom_tag))
        {
            throw new InvalidOperationException($"Tag with name {tagDto.nom_tag} already exists");
        }
        var newTag = _mapper.Map<Tags>(tagDto);
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
            // check if another tag with the name already exists
            if (await _context.Tags.AnyAsync(t => t.nom_tag == tagDto.nom_tag && t.id_tag != id))
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