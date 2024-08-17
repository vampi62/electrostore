using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Tensorflow;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.IAService;

public class IAService : IIAService
{
    private readonly ApplicationDbContext _context;

    public IAService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReadIADto>> GetIA(int limit = 100, int offset = 0)
    {
        return await _context.IA
            .Skip(offset)
            .Take(limit)
            .Select(ia => new ReadIADto
            {
                id_ia = ia.id_ia,
                nom_ia = ia.nom_ia,
                description_ia = ia.description_ia,
                date_ia = ia.date_ia,
                trained_ia = ia.trained_ia
            }).ToListAsync();
    }

    public async Task<ActionResult<ReadIADto>> GetIAById(int id)
    {
        var ia = await _context.IA.FindAsync(id);
        if (ia == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found" } }});
        }

        return new ReadIADto
        {
            id_ia = ia.id_ia,
            nom_ia = ia.nom_ia,
            description_ia = ia.description_ia,
            date_ia = ia.date_ia,
            trained_ia = ia.trained_ia
        };
    }

    public async Task<ReadIADto> CreateIA(CreateIADto iaDto)
    {
        var newIA = new IA
        {
            nom_ia = iaDto.nom_ia,
            description_ia = iaDto.description_ia,
            date_ia = DateTime.Now
        };

        _context.IA.Add(newIA);
        await _context.SaveChangesAsync();

        return new ReadIADto
        {
            id_ia = newIA.id_ia,
            nom_ia = newIA.nom_ia,
            description_ia = newIA.description_ia,
            date_ia = newIA.date_ia,
            trained_ia = newIA.trained_ia
        };
    }

    public async Task<ActionResult<ReadIADto>> UpdateIA(int id, UpdateIADto iaDto)
    {
        var iaToUpdata = await _context.IA.FindAsync(id);
        if (iaToUpdata == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found" } }});
        }

        if (iaDto.nom_ia != null)
        {
            iaToUpdata.nom_ia = iaDto.nom_ia;
        }

        if (iaDto.description_ia != null)
        {
            iaToUpdata.description_ia = iaDto.description_ia;
        }

        await _context.SaveChangesAsync();

        return new ReadIADto
        {
            id_ia = iaToUpdata.id_ia,
            nom_ia = iaToUpdata.nom_ia,
            description_ia = iaToUpdata.description_ia,
            date_ia = iaToUpdata.date_ia,
            trained_ia = iaToUpdata.trained_ia
        };
    }

    public async Task<IActionResult> DeleteIA(int id)
    {
        var iaToDelete = await _context.IA.FindAsync(id);
        if (iaToDelete == null)
        {
            return new BadRequestObjectResult(new { type = "https://tools.ietf.org/html/rfc7231#section-6.5.1", title = "One or more validation errors occurred.", status = 400, errors = new { id_ia = new string[] { "IA not found" } }});
        }

        _context.IA.Remove(iaToDelete);
        await _context.SaveChangesAsync();
        return new OkResult();
    }

    public async Task<ReadItemDto> DetectItem(int id_ia, IFormFile file)
    {
        //TODO

        return new ReadItemDto();
    }

    public async Task<ActionResult<ReadIADto>> TrainIA(int id)
    {
        //TODO

        return new ReadIADto();
    }
}