using AutoMapper;
using ElectrostoreAPI.Dto;
using ElectrostoreAPI.Enums;
using ElectrostoreAPI.Extensions;
using ElectrostoreAPI.Models;
using ElectrostoreAPI.Services.FileService;
using ElectrostoreAPI.Services.SessionService;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ElectrostoreAPI.Services.ZoneService;

public class ZoneService : IZoneService
{
    private readonly IMapper _mapper;
    private readonly ApplicationDbContext _context;
    private readonly ISessionService _sessionService;
    private readonly IFileService _fileService;
    private readonly string _picturesPath = "zones";
    private readonly string _thumbnailsPath = "zonesThumbnails";

    public ZoneService(IMapper mapper, ApplicationDbContext context, ISessionService sessionService, IFileService fileService)
    {
        _mapper = mapper;
        _context = context;
        _sessionService = sessionService;
        _fileService = fileService;
    }

    public async Task<PaginatedResponseDto<ReadExtendedZoneDto>> GetZones(int limit = 100, int offset = 0,
    List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null)
    {
        var query = _context.Zones.AsQueryable();
        var filterResult = default(Expression<Func<Zones, bool>>);
        if (idResearch is not null && idResearch.Count > 0)
        {
            query = query.Where(z => idResearch.Contains(z.id_zone));
        }
        else
        {
            if (rsql != null && rsql.Count > 0)
            {
                (filterResult, rsql) = RsqlParserExtensions.ToFilterExpression<Zones>(rsql);
                query = query.Where(filterResult);
            }
            if (!string.IsNullOrEmpty(sort?.Field))
            {
                var sortResult = RsqlParserExtensions.ToSortExpression<Zones>(sort);
                if (sortResult.Item1 != null)
                {
                    query = sortResult.Item2 == "asc" ? query.OrderBy(sortResult.Item1) : query.OrderByDescending(sortResult.Item1);
                }
                else
                {
                    sort = new SorterDto { Field = "id_zone", Order = "asc" };
                    query = query.OrderBy(z => z.id_zone);
                }
            }
            else
            {
                query = query.OrderBy(z => z.id_zone);
            }
        }
        query = query.Skip(offset).Take(limit);
        var zones = await query
            .Select(z => new
            {
                Zone = z,
                StoresCount = z.Stores.Count,
                Stores = expand != null && expand.Contains("stores") ? z.Stores.Take(20).ToList() : null
            })
            .ToListAsync();
        return new PaginatedResponseDto<ReadExtendedZoneDto>
        {
            data = zones.Select(z =>
            {
                return _mapper.Map<ReadExtendedZoneDto>(z.Zone) with
                {
                    stores_count = z.StoresCount,
                    stores = _mapper.Map<IEnumerable<ReadStoreDto>>(z.Stores)
                };
            }).ToList(),
            pagination = new PaginationDto
            {
                offset = offset,
                limit = limit,
                total = await _context.Zones.CountAsync(filterResult ?? (z => true)),
                nextOffset = offset + limit,
                hasMore = await _context.Zones.Skip(offset + limit).AnyAsync(filterResult ?? (z => true))
            },
            filters = rsql,
            sort = sort != null ? [sort] : null
        };
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
        await _fileService.CreateDirectory(Path.Combine(_picturesPath, newZone.id_zone.ToString()));
        await _fileService.CreateDirectory(Path.Combine(_thumbnailsPath, newZone.id_zone.ToString()));
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
        await _fileService.DeleteDirectory(Path.Combine(_picturesPath, id.ToString()));
        await _fileService.DeleteDirectory(Path.Combine(_thumbnailsPath, id.ToString()));
    }

    public async Task<ReadZoneDto> UploadZonePicture(int id, IFormFile file)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a zone");
        }
        var zoneToUpdate = await _context.Zones.FindAsync(id) ?? throw new KeyNotFoundException($"Zone with id '{id}' not found");
        if (zoneToUpdate.url_picture_zone is not null)
        {
            await _fileService.DeleteFile(zoneToUpdate.url_picture_zone);
        }
        if (zoneToUpdate.url_thumbnail_zone is not null)
        {
            await _fileService.DeleteFile(zoneToUpdate.url_thumbnail_zone);
        }
        var savedPicture = await _fileService.SaveFile(Path.Combine(_picturesPath, id.ToString()), file.FileName, file.ContentType, file.OpenReadStream());
        var savedThumbnail = await _fileService.GenerateThumbnail(
            savedPicture.path,
            Path.Combine(_thumbnailsPath, id.ToString()),
            256, 256);
        zoneToUpdate.url_picture_zone = savedPicture.path;
        zoneToUpdate.url_thumbnail_zone = savedThumbnail.path;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadZoneDto>(zoneToUpdate);
    }

    public async Task<ReadZoneDto> DeleteZonePicture(int id)
    {
        var clientRole = _sessionService.GetClientRole();
        if (clientRole < UserRole.Admin)
        {
            throw new UnauthorizedAccessException("You do not have permission to update a zone");
        }
        var zoneToUpdate = await _context.Zones.FindAsync(id) ?? throw new KeyNotFoundException($"Zone with id '{id}' not found");
        if (zoneToUpdate.url_picture_zone is not null)
        {
            await _fileService.DeleteFile(zoneToUpdate.url_picture_zone);
        }
        if (zoneToUpdate.url_thumbnail_zone is not null)
        {
            await _fileService.DeleteFile(zoneToUpdate.url_thumbnail_zone);
        }
        zoneToUpdate.url_picture_zone = null;
        zoneToUpdate.url_thumbnail_zone = null;
        await _context.SaveChangesAsync();
        return _mapper.Map<ReadZoneDto>(zoneToUpdate);
    }
}
