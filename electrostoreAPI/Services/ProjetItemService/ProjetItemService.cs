using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetItemService;

public class ProjetItemService : IProjetItemService
{
    private readonly ApplicationDbContext _context;

    public ProjetItemService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByProjetId(int ProjetId, int limit = 100, int offset = 0)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == ProjetId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "Projet not found" } } });
        }

        return await _context.ProjetsItems
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_projet == ProjetId)
            .Select(p => new ReadProjetItemDto
            {
                id_item = p.id_item,
                id_projet = p.id_projet,
                qte_projetitem = p.qte_projetitem
            })
            .ToListAsync();
    }

    public async Task<ActionResult<IEnumerable<ReadProjetItemDto>>> GetProjetItemsByItemId(int ItemId, int limit = 100, int offset = 0)
    {
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == ItemId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } } });
        }

        return await _context.ProjetsItems
            .Skip(offset)
            .Take(limit)
            .Where(p => p.id_item == ItemId)
            .Select(p => new ReadProjetItemDto
            {
                id_item = p.id_item,
                id_projet = p.id_projet,
                qte_projetitem = p.qte_projetitem
            })
            .ToListAsync();
    }

    public async Task<ActionResult<ReadProjetItemDto>> GetProjetItemById(int projetId, int itemId)
    {
        var projetItem = await _context.ProjetsItems.FindAsync(projetId, itemId);
        if (projetItem == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "ProjetItem not found" } } });
        }

        return new ReadProjetItemDto
        {
            id_item = projetItem.id_item,
            id_projet = projetItem.id_projet,
            qte_projetitem = projetItem.qte_projetitem
        };
    }

    public async Task<ActionResult<ReadProjetItemDto>> CreateProjetItem(CreateProjetItemDto projetItemDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetItemDto.id_projet))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "Projet not found" } } });
        }
        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == projetItemDto.id_item))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } } });
        }
        // check if the projetItem already exists
        if (await _context.ProjetsItems.AnyAsync(p => p.id_projet == projetItemDto.id_projet && p.id_item == projetItemDto.id_item))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "ProjetItem already exists" } } });
        }

        var newProjetItem = new ProjetsItems
        {
            id_projet = projetItemDto.id_projet,
            id_item = projetItemDto.id_item,
            qte_projetitem = projetItemDto.qte_projetitem
        };

        _context.ProjetsItems.Add(newProjetItem);
        await _context.SaveChangesAsync();

        return new ReadProjetItemDto
        {
            id_item = newProjetItem.id_item,
            id_projet = newProjetItem.id_projet,
            qte_projetitem = newProjetItem.qte_projetitem
        };
    }

    public async Task<ActionResult<ReadProjetItemDto>> UpdateProjetItem(int projetId, int itemId, UpdateProjetItemDto projetItemDto)
    {
        // check if the projet exists
        if (!await _context.Projets.AnyAsync(p => p.id_projet == projetId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "Projet not found" } }});
        }

        // check if the item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_item = new string[] { "Item not found" } }});
        }

        var projetItemToUpdate = await _context.ProjetsItems.FindAsync(projetId, itemId);
        if (projetItemToUpdate == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "ProjetItem not found" } }});
        }

        if (projetItemDto.qte_projetitem != null)
        {
            projetItemToUpdate.qte_projetitem = projetItemDto.qte_projetitem.Value;
        }

        await _context.SaveChangesAsync();

        return new ReadProjetItemDto
        {
            id_item = projetItemToUpdate.id_item,
            id_projet = projetItemToUpdate.id_projet,
            qte_projetitem = projetItemToUpdate.qte_projetitem
        };
    }

    public async Task<IActionResult> DeleteProjetItem(int projetId, int itemId)
    {
        var projetItemToDelete = await _context.ProjetsItems.FindAsync(projetId, itemId);
        if (projetItemToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_projet = new string[] { "ProjetItem not found" } }});
        }
        _context.ProjetsItems.Remove(projetItemToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }
}