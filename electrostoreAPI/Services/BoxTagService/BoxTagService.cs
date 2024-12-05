using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.BoxTagService;

public class BoxTagService : IBoxTagService
{
    private readonly ApplicationDbContext _context;

    public BoxTagService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadBoxTagDto>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0)
    {
        // check if box exists
        if (!await _context.Boxs.AnyAsync(s => s.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found");
        }
        return await _context.BoxsTags
            .Skip(offset)
            .Take(limit)
            .Where(s => s.id_box == boxId)
            .Select(s => new ReadBoxTagDto
            {
                id_box = s.id_box,
                id_tag = s.id_tag
            })
            .ToListAsync();
    }

    public async Task<int> GetBoxsTagsCountByBoxId(int boxId)
    {
        // check if box exists
        if (!await _context.Boxs.AnyAsync(s => s.id_box == boxId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found");
        }
        return await _context.BoxsTags
            .Where(s => s.id_box == boxId)
            .CountAsync();
    }

    public async Task<IEnumerable<ReadBoxTagDto>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(s => s.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        return await _context.BoxsTags
            .Skip(offset)
            .Take(limit)
            .Where(s => s.id_tag == tagId)
            .Select(s => new ReadBoxTagDto
            {
                id_box = s.id_box,
                id_tag = s.id_tag
            })
            .ToListAsync();
    }

    public async Task<int> GetBoxsTagsCountByTagId(int tagId)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(s => s.id_tag == tagId))
        {
            throw new KeyNotFoundException($"Tag with id {tagId} not found");
        }
        return await _context.BoxsTags
            .Where(s => s.id_tag == tagId)
            .CountAsync();
    }

    public async Task<ReadBoxTagDto> GetBoxTagById(int boxId, int tagId)
    {
        var boxTag = await _context.BoxsTags.FindAsync(boxId, tagId) ?? throw new KeyNotFoundException($"BoxTag with id {boxId} and {tagId} not found");
        return new ReadBoxTagDto
        {
            id_box = boxTag.id_box,
            id_tag = boxTag.id_tag
        };
    }

    public async Task<IEnumerable<ReadBoxTagDto>> CreateBoxTags(int? boxId = null, int? tagId = null, int[]? tags = null, int[]? boxs = null)
    {
        var newBoxTagList = new List<BoxsTags>();
        if (boxId != null && tags != null)
        {
            // check if box exists
            if (!await _context.Boxs.AnyAsync(s => s.id_box == boxId.Value))
            {
                throw new KeyNotFoundException($"Box with id {boxId.Value} not found");
            }
            // check if all tags exist
            for (int i = 0; i < tags.Length; i++)
            {
                if (!await _context.Tags.AnyAsync(s => s.id_tag == tags[i]))
                {
                    throw new KeyNotFoundException($"Tag with id {tags[i]} not found");
                }
            }
            // create the boxtags
            for (int i = 0; i < tags.Length; i++)
            {
                if (await _context.BoxsTags.AnyAsync(s => s.id_box == boxId.Value && s.id_tag == tags[i]))
                {
                    throw new InvalidOperationException($"BoxTag with id {boxId.Value} and {tags[i]} already exists");
                }
                var newBoxTag = new BoxsTags
                {
                    id_box = boxId.Value,
                    id_tag = tags[i]
                };
                _context.BoxsTags.Add(newBoxTag);
                newBoxTagList.Add(newBoxTag);
            }
            await _context.SaveChangesAsync();
        }
        else if (tagId != null && boxs != null)
        {
            // check if tag exists
            if (!await _context.Tags.AnyAsync(s => s.id_tag == tagId.Value))
            {
                throw new KeyNotFoundException($"Tag with id {tagId.Value} not found");
            }
            // check if all boxs exist
            for (int i = 0; i < boxs.Length; i++)
            {
                if (!await _context.Boxs.AnyAsync(s => s.id_box == boxs[i]))
                {
                    throw new KeyNotFoundException($"Box with id {boxs[i]} not found");
                }
            }
            // create the boxtags
            for (int i = 0; i < boxs.Length; i++)
            {
                if (await _context.BoxsTags.AnyAsync(s => s.id_box == boxs[i] && s.id_tag == tagId.Value))
                {
                    throw new InvalidOperationException($"BoxTag with id {boxs[i]} and {tagId.Value} already exists");
                }
                var newBoxTag = new BoxsTags
                {
                    id_box = boxs[i],
                    id_tag = tagId.Value
                };
                _context.BoxsTags.Add(newBoxTag);
                newBoxTagList.Add(newBoxTag);
            }
            await _context.SaveChangesAsync();
        }
        else
        {
            throw new NotImplementedException();
        }
        return newBoxTagList.Select(s => new ReadBoxTagDto
        {
            id_box = s.id_box,
            id_tag = s.id_tag
        });
    }

    public async Task<ReadBoxTagDto> CreateBoxTag(CreateBoxTagDto boxTagDto)
    {
        // check if box exists
        if (!await _context.Boxs.AnyAsync(s => s.id_box == boxTagDto.id_box))
        {
            throw new KeyNotFoundException($"Box with id {boxTagDto.id_box} not found");
        }
        // check if tag exists
        if (!await _context.Tags.AnyAsync(s => s.id_tag == boxTagDto.id_tag))
        {
            throw new KeyNotFoundException($"Tag with id {boxTagDto.id_tag} not found");
        }
        // check if the boxtag already exists
        if (await _context.BoxsTags.AnyAsync(s => s.id_box == boxTagDto.id_box && s.id_tag == boxTagDto.id_tag))
        {
            throw new InvalidOperationException($"BoxTag with id {boxTagDto.id_box} and {boxTagDto.id_tag} already exists");
        }
        var newBoxTag = new BoxsTags
        {
            id_box = boxTagDto.id_box,
            id_tag = boxTagDto.id_tag
        };
        _context.BoxsTags.Add(newBoxTag);
        await _context.SaveChangesAsync();
        return new ReadBoxTagDto
        {
            id_box = newBoxTag.id_box,
            id_tag = newBoxTag.id_tag
        };
    }

    public async Task DeleteBoxTag(int boxId, int tagId)
    {
        var boxTagToDelete = await _context.BoxsTags.FindAsync(boxId, tagId) ?? throw new KeyNotFoundException($"BoxTag with id {boxId} and {tagId} not found");
        _context.BoxsTags.Remove(boxTagToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task CheckIfStoreExists(int storeId)
    {
        if (!await _context.Stores.AnyAsync(store => store.id_store == storeId))
        {
            throw new KeyNotFoundException($"Store with id {storeId} not found");
        }
    }
}