using ElectrostoreAPI.Dto;
using Microsoft.AspNetCore.Http;

namespace ElectrostoreAPI.Services.ZoneService;

public interface IZoneService
{
    public Task<PaginatedResponseDto<ReadExtendedZoneDto>> GetZones(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedZoneDto> GetZoneById(int id, List<string>? expand = null);

    public Task<ReadZoneDto> CreateZone(CreateZoneDto zoneDto);

    public Task<ReadZoneDto> UpdateZone(int id, UpdateZoneDto zoneDto);

    public Task DeleteZone(int id);

    public Task<ReadZoneDto> UploadZonePicture(int id, IFormFile file);

    public Task<ReadZoneDto> DeleteZonePicture(int id);
}
