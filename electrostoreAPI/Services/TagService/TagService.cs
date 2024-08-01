using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.TagService;

public class TagService : ITagService
{
    private readonly ApplicationDbContext _context;

    public TagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadTagDto>> GetTags(int limit = 100, int offset = 0)
    {
        return await _context.Tags
            .Skip(offset)
            .Take(limit)
            .Select(t => new ReadTagDto
            {
                id_tag = t.id_tag,
                nom_tag = t.nom_tag,
                poids_tag = t.poids_tag
            })
            .ToListAsync();
    }

    public async Task<ReadTagDto> GetTagById(int id)
    {
        var tag = await _context.Tags.FindAsync(id);

        if (tag == null)
        {
            throw new ArgumentException("Tag not found");
        }

        return new ReadTagDto
        {
            id_tag = tag.id_tag,
            nom_tag = tag.nom_tag,
            poids_tag = tag.poids_tag
        };
    }

    public async Task<ReadTagDto> CreateTag(CreateTagDto tagDto)
    {
        var newTag = new Tags
        {
            nom_tag = tagDto.nom_tag,
            poids_tag = tagDto.poids_tag
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

    public async Task<ReadTagDto> UpdateTag(int id, UpdateTagDto tagDto)
    {
        var tagToUpdate = await _context.Tags.FindAsync(id);

        if (tagToUpdate == null)
        {
            throw new ArgumentException("Tag not found");
        }

        if (tagDto.nom_tag != null)
        {
            tagToUpdate.nom_tag = tagDto.nom_tag;
        }

        if (tagDto.poids_tag != null)
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
        var tagToDelete = await _context.Tags.FindAsync(id);

        if (tagToDelete == null)
        {
            throw new ArgumentException("Tag not found");
        }

        _context.Tags.Remove(tagToDelete);
        await _context.SaveChangesAsync();
    }
}