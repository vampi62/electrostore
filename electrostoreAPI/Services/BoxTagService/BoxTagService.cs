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

    public async Task<ActionResult<IEnumerable<ReadBoxTagDto>>> GetBoxsTagsByBoxId(int boxId, int limit = 100, int offset = 0)
    {
        // check if box exists
        if (!await _context.Boxs.AnyAsync(s => s.id_box == boxId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { boxId = new string[] { "Box not found" } } });
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

    public async Task<ActionResult<IEnumerable<ReadBoxTagDto>>> GetBoxsTagsByTagId(int tagId, int limit = 100, int offset = 0)
    {
        // check if tag exists
        if (!await _context.Tags.AnyAsync(s => s.id_tag == tagId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { tagId = new string[] { "Tag not found" } } });
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

    public async Task<ActionResult<ReadBoxTagDto>> GetBoxTagById(int boxId, int tagId)
    {
        var boxTag = await _context.BoxsTags.FindAsync(boxId, tagId);
        if (boxTag == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", title = "Not Found", status = 404, detail = "BoxTag not found" });
        }

        return new ReadBoxTagDto
        {
            id_box = boxTag.id_box,
            id_tag = boxTag.id_tag
        };
    }

    public async Task<ActionResult<ReadBoxTagDto>> CreateBoxTag(CreateBoxTagDto boxTagDto)
    {
        // check if box exists
        if (!await _context.Boxs.AnyAsync(s => s.id_box == boxTagDto.id_box))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { boxId = new string[] { "Box not found" } }});
        }
        
        // check if tag exists
        if (!await _context.Tags.AnyAsync(s => s.id_tag == boxTagDto.id_tag))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { tagId = new string[] { "Tag not found" } }});
        }

        // check if the boxtag already exists
        if (await _context.BoxsTags.AnyAsync(s => s.id_box == boxTagDto.id_box && s.id_tag == boxTagDto.id_tag))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { boxTag = new string[] { "BoxTag already exists" } }});
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

    public async Task<IActionResult> DeleteBoxTag(int boxId, int tagId)
    {
        var boxTagToDelete = await _context.BoxsTags.FindAsync(boxId, tagId);
        if (boxTagToDelete == null)
        {
            return new NotFoundObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.4", title = "Not Found", status = 404, detail = "BoxTag not found" });
        }

        _context.BoxsTags.Remove(boxTagToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}