using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;

namespace electrostore.Services.ImgService;

public class ImgService : IImgService
{
    private readonly ApplicationDbContext _context;

    public ImgService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadImgDto>> GetImgs(int limit = 100, int offset = 0)
    {
        return await _context.Imgs
            .Select(img => new ReadImgDto
            {
                id_img = img.id_img,
                nom_img = img.nom_img,
                url_img = img.url_img,
                description_img = img.description_img,
                date_img = img.date_img,
                id_item = img.id_item
            })
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReadImgDto>> GetImgsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        //check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new ArgumentException("Item not found");
        }

        return await _context.Imgs
            .Skip(offset)
            .Take(limit)
            .Where(img => img.id_item == itemId)
            .Select(img => new ReadImgDto
            {
                id_img = img.id_img,
                nom_img = img.nom_img,
                url_img = img.url_img,
                description_img = img.description_img,
                date_img = img.date_img,
                id_item = img.id_item
            }).ToListAsync();
    }

    public async Task<ReadImgDto> GetImgById(int id, int? itemId = null)
    {
        var img = await _context.Imgs.FindAsync(id);
        if (img == null)
        {
            throw new ArgumentException("Img not found");
        }
        if (itemId != null && img.id_item != itemId)
        {
            throw new ArgumentException("Img not found");
        }

        return new ReadImgDto
        {
            id_img = img.id_img,
            nom_img = img.nom_img,
            url_img = img.url_img,
            description_img = img.description_img,
            date_img = img.date_img,
            id_item = img.id_item
        };
    }

    public async Task<ReadImgDto> CreateImg(CreateImgDto imgDto)
    {
        // check if item exists
        var item = await _context.Items.FindAsync(imgDto.id_item);
        if (item == null)
        {
            throw new ArgumentException("Item not found");
        }

        var newImg = new Imgs
        {
            nom_img = imgDto.nom_img,
            url_img = imgDto.url_img,
            description_img = imgDto.description_img,
            date_img = DateTime.Now,
            id_item = imgDto.id_item
        };

        _context.Imgs.Add(newImg);
        await _context.SaveChangesAsync();

        return new ReadImgDto
        {
            id_img = newImg.id_img,
            nom_img = newImg.nom_img,
            url_img = newImg.url_img,
            description_img = newImg.description_img,
            date_img = newImg.date_img,
            id_item = newImg.id_item
        };
    }

    public async Task<ReadImgDto> UpdateImg(int id, UpdateImgDto imgDto, int? itemId = null)
    {
        var imgToUpdate = await _context.Imgs.FindAsync(id);
        if (imgToUpdate == null)
        {
            throw new ArgumentException("Img not found");
        }
        if (itemId != null && imgToUpdate.id_item != itemId)
        {
            throw new ArgumentException("Img not found");
        }

        if (imgDto.nom_img != null)
        {
            imgToUpdate.nom_img = imgDto.nom_img;
        }

        if (imgDto.description_img != null)
        {
            imgToUpdate.description_img = imgDto.description_img;
        }

        await _context.SaveChangesAsync();

        return new ReadImgDto
        {
            id_img = imgToUpdate.id_img,
            nom_img = imgToUpdate.nom_img,
            url_img = imgToUpdate.url_img,
            description_img = imgToUpdate.description_img,
            date_img = imgToUpdate.date_img,
            id_item = imgToUpdate.id_item
        };
    }

    public async Task DeleteImg(int id, int? itemId = null)
    {
        var imgToDelete = await _context.Imgs.FindAsync(id);
        if (imgToDelete == null)
        {
            throw new ArgumentException("Img not found");
        }
        if (itemId != null && imgToDelete.id_item != itemId)
        {
            throw new ArgumentException("Img not found");
        }

        _context.Imgs.Remove(imgToDelete);
        await _context.SaveChangesAsync();
    }
}