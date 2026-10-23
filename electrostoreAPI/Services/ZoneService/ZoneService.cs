using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.EntityFrameworkCore;

namespace ElectrostoreAPI.Services.ZoneService;

public class ZoneService : IZoneService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IFileService _fileService;
    private readonly string _zoneImagesPath = "zoneImages";
    private readonly string _zoneImagesThumbnailsPath = "zoneImagesThumbnails";

    public ZoneService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedZoneDto>> GetZones(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null)
    {
        var query = _context.Zones.AsQueryable();
        return await query
            .ToPagedQuery(limit, offset, rsql, sort, new SorterDto { field = "id_zone", order = "asc" })
            .ToProjectedResponseAsync(
                z => new
                {
                    Zone = z,
                    StoresCount = z.Stores.Count,
                    Stores = expand != null && expand.Contains("stores") ? z.Stores.Take(20).ToList() : null
                },
                zones => zones.Select(z =>
                {
                    return _mapper.Map<ReadExtendedZoneDto>(z.Zone) with
                    {
                        stores_count = z.StoresCount,
                        stores = _mapper.Map<IEnumerable<ReadStoreDto>>(z.Stores)
                    };
                }).ToList());
    }

    public async Task<ReadExtendedZoneDto> GetZoneById(int id, List<string>? expand = null)
    {
        var query = _context.Zones.AsQueryable();
        query = query.Where(z => z.id_zone == id);
        var zone = await query
            .Select(z => new
            {
                Zone = z,
                StoresCount = z.Stores.Count,
                Stores = expand != null && expand.Contains("stores") ? z.Stores.Take(20).ToList() : null
            })
            .FirstOrDefaultAsync() ?? throw new KeyNotFoundException($"Zone with id '{id}' not found");
        return _mapper.Map<ReadExtendedZoneDto>(zone.Zone) with
        {
            stores_count = zone.StoresCount,
            stores = _mapper.Map<IEnumerable<ReadStoreDto>>(zone.Stores)
        };
    }

    public async Task<ReadZoneDto> CreateZone(CreateZoneDto zoneDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to create a zone");
        }
        var newZone = _mapper.Map<Zones>(zoneDto);
        _context.Zones.Add(newZone);
        await _context.SaveChangesAsync();
        await _fileService.CreateDirectory(Path.Combine(_zoneImagesPath, newZone.id_zone.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_zoneImagesThumbnailsPath, newZone.id_zone.ToString()));
        if (zoneDto.img_file is not null)
        {
            var (picturePath, thumbnailPath) = await _fileService.SaveImageWithThumbnail(Path.Combine(_zoneImagesPath, newZone.id_zone.ToString()), Path.Combine(_zoneImagesThumbnailsPath, newZone.id_zone.ToString()), zoneDto.img_file);
            newZone.url_picture_zone = picturePath;
            newZone.url_thumbnail_zone = thumbnailPath;
            await _context.SaveChangesAsync();
        }
        return _mapper.Map<ReadZoneDto>(newZone);
    }

    public async Task<ReadZoneDto> UpdateZone(int id, UpdateZoneDto zoneDto)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a zone");
        }
        var zoneToUpdate = await _context.Zones.FindAsync(id) ?? throw new KeyNotFoundException($"Zone with id '{id}' not found");
        if (zoneDto.name_zone is not null)
        {
            zoneToUpdate.name_zone = zoneDto.name_zone;
        }
        if (zoneDto.description_zone is not null)
        {
            zoneToUpdate.description_zone = zoneDto.description_zone;
        }
        if (zoneDto.xlength_zone is not null || zoneDto.ylength_zone is not null)
        {
            var newXlength = zoneDto.xlength_zone ?? zoneToUpdate.xlength_zone;
            var newYlength = zoneDto.ylength_zone ?? zoneToUpdate.ylength_zone;
            // check that every store already placed on this zone's plan still fits within the new size
            if (await _context.Stores.AnyAsync(s => s.id_zone == id && ((s.xmax_store != null && s.xmax_store > newXlength) || (s.ymax_store != null && s.ymax_store > newYlength))))
            {
                throw new ArgumentException("you can't reduce the zone size, a store will be out of zone bounds");
            }
            zoneToUpdate.xlength_zone = newXlength;
            zoneToUpdate.ylength_zone = newYlength;
        }
        if (zoneDto.unset_img_zone is true)
        {
            await _fileService.DeleteImageWithThumbnail(zoneToUpdate.url_picture_zone, zoneToUpdate.url_thumbnail_zone);
            zoneToUpdate.url_picture_zone = null;
            zoneToUpdate.url_thumbnail_zone = null;
        }
        else if (zoneDto.img_file is not null)
        {
            await _fileService.DeleteImageWithThumbnail(zoneToUpdate.url_picture_zone, zoneToUpdate.url_thumbnail_zone);
            var (picturePath, thumbnailPath) = await _fileService.SaveImageWithThumbnail(Path.Combine(_zoneImagesPath, id.ToString()), Path.Combine(_zoneImagesThumbnailsPath, id.ToString()), zoneDto.img_file);
            zoneToUpdate.url_picture_zone = picturePath;
            zoneToUpdate.url_thumbnail_zone = thumbnailPath;
        }
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadZoneDto>(zoneToUpdate);
    }

    public async Task DeleteZone(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to delete a zone");
        }
        var zoneToDelete = await _context.Zones.FindAsync(id) ?? throw new KeyNotFoundException($"Zone with id '{id}' not found");
        _context.Zones.Remove(zoneToDelete);
        await _context.SaveChangesAsync();
        await _fileService.DeleteDirectory(Path.Combine(_zoneImagesPath, id.ToString()));
        await _fileService.DeleteDirectory(Path.Combine(_zoneImagesThumbnailsPath, id.ToString()));
    }
}
