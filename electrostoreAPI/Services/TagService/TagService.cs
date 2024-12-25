using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.TagService;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _context;

    public TagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadExtendedTagDto>> GetTags(int limit = 100, int offset = 0, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Tags.AsQueryable();
        if (idResearch != null && idResearch.Any())
        {
            query = query.Where(b => idResearch.Contains(b.id_tag));
        }
        return await query
            .Skip(offset)
            .Take(limit)
            .Select(t => new ReadExtendedTagDto
            {
                id_tag = t.id_tag,
                nom_tag = t.nom_tag,
                poids_tag = t.poids_tag,
                stores_tags = expand != null && expand.Contains("stores_tags") ? t.StoresTags.Select(st => new ReadStoreTagDto
                {
                    id_store = st.id_store,
                    id_tag = st.id_tag
                }).ToArray() : null,
                items_tags = expand != null && expand.Contains("items_tags") ? t.ItemsTags.Select(it => new ReadItemTagDto
                {
                    id_item = it.id_item,
                    id_tag = it.id_tag
                }).ToArray() : null,
                boxs_tags = expand != null && expand.Contains("boxs_tags") ? t.BoxsTags.Select(bt => new ReadBoxTagDto
                {
                    id_box = bt.id_box,
                    id_tag = bt.id_tag
                }).ToArray() : null,
                stores_tags_count = t.StoresTags.Count,
                items_tags_count = t.ItemsTags.Count,
                boxs_tags_count = t.BoxsTags.Count
            }).ToListAsync();
    }

    public async Task<int> GetTagsCount()
    {
        return await _context.Tags.CountAsync();
    }

    public async Task<ReadExtendedTagDto> GetTagById(int id, List<string>? expand = null)
    {
        return await _context.Tags
            .Where(t => t.id_tag == id)
            .Select(t => new ReadExtendedTagDto
            {
                id_tag = t.id_tag,
                nom_tag = t.nom_tag,
                poids_tag = t.poids_tag,
                stores_tags = expand != null && expand.Contains("stores_tags") ? t.StoresTags.Select(st => new ReadStoreTagDto
                {
                    id_store = st.id_store,
                    id_tag = st.id_tag
                }).ToArray() : null,
                items_tags = expand != null && expand.Contains("items_tags") ? t.ItemsTags.Select(it => new ReadItemTagDto
                {
                    id_item = it.id_item,
                    id_tag = it.id_tag
                }).ToArray() : null,
                boxs_tags = expand != null && expand.Contains("boxs_tags") ? t.BoxsTags.Select(bt => new ReadBoxTagDto
                {
                    id_box = bt.id_box,
                    id_tag = bt.id_tag
                }).ToArray() : null,
                stores_tags_count = t.StoresTags.Count,
                items_tags_count = t.ItemsTags.Count,
                boxs_tags_count = t.BoxsTags.Count
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Tag with id {id} not found");
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
        return new ReadTagDto
        {
            id_tag = newTag.id_tag,
            nom_tag = newTag.nom_tag,
            poids_tag = newTag.poids_tag
        };
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
        return new ReadTagDto
        {
            id_tag = tagToUpdate.id_tag,
            nom_tag = tagToUpdate.nom_tag,
            poids_tag = tagToUpdate.poids_tag
        };
    }

    public async Task DeleteTag(int id)
    {
        var tagToDelete = await _context.Tags.FindAsync(id) ?? throw new KeyNotFoundException($"Tag with id {id} not found");
        _context.Tags.Remove(tagToDelete);
        await _context.SaveChangesAsync();
    }
}