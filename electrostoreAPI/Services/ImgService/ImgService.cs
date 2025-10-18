using AutoMapper;
using Microsoft.EntityFrameworkCore;
using electrostore.Dto;
using electrostore.Models;
using electrostore.Services.FileService;

namespace electrostore.Services.ImgService;

public class ImgService : IImgService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;
    private readonly string _imagesPath = "images";
    private readonly string _imagesThumbnailsPath = "imagesThumbnails";

    public ImgService(IMapper mapper, ApplicationDbContext context, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _fileService = fileService;
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
        if (await _context.Items.FindAsync(imgDto.id_item) is null)
        {
            throw new KeyNotFoundException($"Item with id {imgDto.id_item} not found");
        }
        var savedImg = await _fileService.SaveFile(Path.Combine(_imagesPath, imgDto.id_item.ToString()), imgDto.img_file);
        var savedThumbnail = await _fileService.GenerateThumbnail(
            savedImg.url,
            Path.Combine(_imagesThumbnailsPath, imgDto.id_item.ToString()),
            256, 256);
        var newImg = new Imgs
        {
            nom_img = imgDto.nom_img,
            url_picture_img = savedImg.url,
            url_thumbnail_img = savedThumbnail.url,
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
        await _fileService.DeleteFile(imgToDelete.url_picture_img);
        await _fileService.DeleteFile(imgToDelete.url_thumbnail_img);
        _context.Imgs.Remove(imgToDelete);
        await _context.SaveChangesAsync();
    }
}