using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ImgService;

public class ImgService : IImgService
{
    private readonly ApplicationDbContext _context;

    public ImgService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ReadImgDto>> GetImgsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        //check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
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

    public async Task<int> GetImgsCountByItemId(int itemId)
    {
        //check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        return await _context.Imgs
            .Where(img => img.id_item == itemId)
            .CountAsync();
    }

    public async Task<ReadImgDto> GetImgById(int id, int? itemId = null)
    {
        var img = await _context.Imgs.FindAsync(id);
        if ((img is null) || (itemId is not null && img.id_item != itemId))
        {
            throw new KeyNotFoundException($"Image with id {id} not found");
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
        var item = await _context.Items.FindAsync(imgDto.id_item) ?? throw new KeyNotFoundException($"Item with id {imgDto.id_item} not found");
        var fileName = Path.GetFileNameWithoutExtension(imgDto.img_file.FileName);
        var fileExt = Path.GetExtension(imgDto.img_file.FileName);
        var i = 1;
        // verifie si une image avec le meme nom existe deja sur le serveur dans "wwwroot/images"
        // si oui, on ajoute un numero a la fin du nom de l'image et on recommence la verification jusqu'a trouver un nom disponible
        var newName = imgDto.img_file.FileName;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgDto.id_item.ToString(), newName)))
        {
            newName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgDto.id_item.ToString(), newName);
        using (var fileStream = new FileStream(savePath, FileMode.Create))
        {
            await imgDto.img_file.CopyToAsync(fileStream);
        }
        var newImg = new Imgs
        {
            nom_img = imgDto.nom_img,
            url_img = imgDto.id_item + "/" + newName,
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
        if ((imgToUpdate is null) || (itemId is not null && imgToUpdate.id_item != itemId))
        {
            throw new KeyNotFoundException($"Image with id {id} not found");
        }
        if (imgDto.nom_img is not null)
        {
            imgToUpdate.nom_img = imgDto.nom_img;
        }
        if (imgDto.description_img is not null)
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
        if ((imgToDelete is null) || (itemId is not null && imgToDelete.id_item != itemId))
        {
            throw new KeyNotFoundException($"Image with id {id} not found");
        }
        _context.Imgs.Remove(imgToDelete);
        // supprimer l'image sur le disque
        File.Delete(imgToDelete.url_img);
        await _context.SaveChangesAsync();
    }

    public async Task<GetFileResult> GetImageFile(string url)
    {
        var pathImg = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", url);
        if (!File.Exists(pathImg))
        {
            return new GetFileResult
            {
                Success = false,
                ErrorMessage = "File not found",
                FilePath = "",
                MimeType = ""
            };
        } else {
            var ext = Path.GetExtension(pathImg);
            var mimeType = ext switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
            return await Task.FromResult(new GetFileResult
            {
                Success = true,
                FilePath = pathImg,
                MimeType = mimeType
            });
        }
    }
}