using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.IAImgService;

public class IAImgService : IIAImgService
{
    private readonly ApplicationDbContext _context;

    public IAImgService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadIAImgDto>> GetIAImgByIAId(int idIA, int limit = 100, int offset = 0)
    {
        // check if IA exists
        if (!await _context.IA.AnyAsync(ia => ia.id_ia == idIA))
        {
            throw new ArgumentException("IA not found");
        }

        return await _context.IAImgs
            .Skip(offset)
            .Take(limit)
            .Where(iaimg => iaimg.id_ia == idIA)
            .Select(iaimg => new ReadIAImgDto
            {
                id_ia = iaimg.id_ia,
                id_img = iaimg.id_img
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadIAImgDto>> GetIAImgByImgId(int idImg, int limit = 100, int offset = 0)
    {
        // check if Img exists
        if (!await _context.Imgs.AnyAsync(img => img.id_img == idImg))
        {
            throw new ArgumentException("Img not found");
        }

        return await _context.IAImgs
            .Skip(offset)
            .Take(limit)
            .Where(iaimg => iaimg.id_img == idImg)
            .Select(iaimg => new ReadIAImgDto
            {
                id_ia = iaimg.id_ia,
                id_img = iaimg.id_img
            })
            .ToListAsync();
    }

    public async Task<ReadIAImgDto> GetIAImgById(int idIA, int idImg)
    {
        var IAImg = await _context.IAImgs.FindAsync(idIA, idImg);
        if (IAImg == null)
        {
            throw new ArgumentException("IAImg not found");
        }

        return new ReadIAImgDto
        {
            id_ia = IAImg.id_ia,
            id_img = IAImg.id_img
        };
    }

    public async Task<ReadIAImgDto> CreateIAImg(CreateIAImgDto IAImgDto)
    {
        // check if IA exists
        if (!await _context.IA.AnyAsync(ia => ia.id_ia == IAImgDto.id_ia))
        {
            throw new ArgumentException("IA not found");
        }

        // check if Img exists
        if (!await _context.Imgs.AnyAsync(img => img.id_img == IAImgDto.id_img))
        {
            throw new ArgumentException("Img not found");
        }

        // check if IAImg already exists
        if (await _context.IAImgs.AnyAsync(iaimg => iaimg.id_ia == IAImgDto.id_ia && iaimg.id_img == IAImgDto.id_img))
        {
            throw new ArgumentException("IAImg already exists");
        }

        var newIAImg = new IAImgs
        {
            id_ia = IAImgDto.id_ia,
            id_img = IAImgDto.id_img
        };

        _context.IAImgs.Add(newIAImg);
        await _context.SaveChangesAsync();

        return new ReadIAImgDto
        {
            id_ia = newIAImg.id_ia,
            id_img = newIAImg.id_img
        };
    }

    public async Task DeleteIAImg(int idIA, int idImg)
    {
        // check if IAImg exists
        var IAImgToDelete = await _context.IAImgs.FindAsync(idIA, idImg);
        if (IAImgToDelete == null)
        {
            throw new ArgumentException("IAImg not found");
        }

        _context.IAImgs.Remove(IAImgToDelete);
        await _context.SaveChangesAsync();
    }
}