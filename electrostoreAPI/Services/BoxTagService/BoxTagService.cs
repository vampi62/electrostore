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

    public async Task<IEnumerable<ReadExtendedBoxTagDto>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0, List<string>? expand = null)
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
            .Select(s => new ReadExtendedBoxTagDto
            {
                id_box = s.id_box,
                id_tag = s.id_tag,
                tag = expand != null && expand.Contains("tag") ? new ReadTagDto
                {
                    id_tag = s.Tag.id_tag,
                    nom_tag = s.Tag.nom_tag
                } : null,
                box = expand != null && expand.Contains("box") ? new ReadBoxDto
                {
                    id_box = s.Box.id_box,
                    id_store = s.Box.id_store,
                    xstart_box = s.Box.xstart_box,
                    ystart_box = s.Box.ystart_box,
                    xend_box = s.Box.xend_box,
                    yend_box = s.Box.yend_box
                } : null
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

    public async Task<IEnumerable<ReadExtendedBoxTagDto>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0, List<string>? expand = null)
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
            .Select(s => new ReadExtendedBoxTagDto
            {
                id_box = s.id_box,
                id_tag = s.id_tag,
                tag = expand != null && expand.Contains("tag") ? new ReadTagDto
                {
                    id_tag = s.Tag.id_tag,
                    nom_tag = s.Tag.nom_tag
                } : null,
                box = expand != null && expand.Contains("box") ? new ReadBoxDto
                {
                    id_box = s.Box.id_box,
                    id_store = s.Box.id_store,
                    xstart_box = s.Box.xstart_box,
                    ystart_box = s.Box.ystart_box,
                    xend_box = s.Box.xend_box,
                    yend_box = s.Box.yend_box
                } : null
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

    public async Task<ReadExtendedBoxTagDto> GetBoxTagById(int boxId, int tagId, List<string>? expand = null)
    {
        var boxTag = await _context.BoxsTags.FindAsync(boxId, tagId) ?? throw new KeyNotFoundException($"BoxTag with id {boxId} and {tagId} not found");
        return new ReadExtendedBoxTagDto
        {
            id_box = boxTag.id_box,
            id_tag = boxTag.id_tag,
            tag = expand != null && expand.Contains("tag") ? new ReadTagDto
            {
                id_tag = boxTag.Tag.id_tag,
                nom_tag = boxTag.Tag.nom_tag
            } : null,
            box = expand != null && expand.Contains("box") ? new ReadBoxDto
            {
                id_box = boxTag.Box.id_box,
                id_store = boxTag.Box.id_store,
                xstart_box = boxTag.Box.xstart_box,
                ystart_box = boxTag.Box.ystart_box,
                xend_box = boxTag.Box.xend_box,
                yend_box = boxTag.Box.yend_box
            } : null
        };
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

    public async Task<ReadBulkBoxTagDto> CreateBulkBoxTag(List<CreateBoxTagDto> boxTagBulkDto)
    {
        var validQuery = new List<ReadBoxTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxTagDto in boxTagBulkDto)
        {
            try
            {
                validQuery.Add(await CreateBoxTag(boxTagDto));
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxTagDto
                });
            }
        }
        return new ReadBulkBoxTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }

    public async Task DeleteBoxTag(int boxId, int tagId)
    {
        var boxTagToDelete = await _context.BoxsTags.FindAsync(boxId, tagId) ?? throw new KeyNotFoundException($"BoxTag with id {boxId} and {tagId} not found");
        _context.BoxsTags.Remove(boxTagToDelete);
        await _context.SaveChangesAsync();
    }

    public async Task CheckIfStoreExists(int storeId, int boxId)
    {
        if (!await _context.Boxs.AnyAsync(box => box.id_box == boxId && box.id_store == storeId))
        {
            throw new KeyNotFoundException($"Box with id {boxId} not found in store with id {storeId}");
        }
    }

    public async Task<ReadBulkBoxTagDto> DeleteBulkItemTag(List<CreateBoxTagDto> boxTagBulkDto)
    {
        var validQuery = new List<ReadBoxTagDto>();
        var errorQuery = new List<ErrorDetail>();
        foreach (var boxTagDto in boxTagBulkDto)
        {
            try
            {
                await DeleteBoxTag(boxTagDto.id_box, boxTagDto.id_tag);
                validQuery.Add(new ReadBoxTagDto
                {
                    id_box = boxTagDto.id_box,
                    id_tag = boxTagDto.id_tag
                });
            }
            catch (Exception e)
            {
                errorQuery.Add(new ErrorDetail
                {
                    Reason = e.Message,
                    Data = boxTagDto
                });
            }
        }
        return new ReadBulkBoxTagDto
        {
            Valide = validQuery,
            Error = errorQuery
        };
    }
}