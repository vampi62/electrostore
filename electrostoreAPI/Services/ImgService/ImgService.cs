using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace electrostore.Services.ImgService;

public class ImgService : IImgService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;

    public ImgService(IMapper mapper, ApplicationDbContext context)
    {
        _mapper = mapper;
        _context = context;
    }

    public async Task<IEnumerable<ReadImgDto>> GetImgsByItemId(int itemId, int limit = 100, int offset = 0)
    {
        //check if item exists
        if (!await _context.Items.AnyAsync(i => i.id_item == itemId))
        {
            throw new KeyNotFoundException($"Item with id {itemId} not found");
        }
        var query = _context.Imgs.AsQueryable();
        query = query.Where(im => im.id_item == itemId);
        query = query.Skip(offset).Take(limit);
        query = query.OrderBy(im => im.id_img);
        var img = await query.ToListAsync();
        return _mapper.Map<List<ReadImgDto>>(img);
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
        return _mapper.Map<ReadImgDto>(img);
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
        var pictureName = imgDto.img_file.FileName;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgDto.id_item.ToString(), pictureName)))
        {
            pictureName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var picturePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgDto.id_item.ToString(), pictureName);
        using (var fileStream = new FileStream(picturePath, FileMode.Create))
        {
            await imgDto.img_file.CopyToAsync(fileStream);
        }

        var thumbnailName = "thumbnail_" + imgDto.img_file.FileName;
        while (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgDto.id_item.ToString(), thumbnailName)))
        {
            thumbnailName = $"{fileName}({i}){fileExt}";
            i++;
        }
        var thumbnailPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgDto.id_item.ToString(), thumbnailName);
        using (var image = await Image.LoadAsync(picturePath))
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(1024, 1024),
                Mode = ResizeMode.Max
            }));

            await image.SaveAsJpegAsync(thumbnailPath);
        }
        var newImg = new Imgs
        {
            nom_img = imgDto.nom_img,
            url_picture_img = imgDto.id_item + "/" + pictureName,
            url_thumbnail_img = imgDto.id_item + "/" + thumbnailName,
            description_img = imgDto.description_img,
            id_item = imgDto.id_item
        };
        _context.Imgs.Add(newImg);
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadImgDto>(newImg);
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
        return _mapper.Map<ReadImgDto>(imgToUpdate);
    }

    public async Task DeleteImg(int id, int? itemId = null)
    {
        var imgToDelete = await _context.Imgs.FindAsync(id);
        if ((imgToDelete is null) || (itemId is not null && imgToDelete.id_item != itemId))
        {
            throw new KeyNotFoundException($"Image with id {id} not found");
        }
        _context.Imgs.Remove(imgToDelete);
        // supprimer les images sur le disque
        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgToDelete.url_picture_img));
        File.Delete(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", imgToDelete.url_thumbnail_img));
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