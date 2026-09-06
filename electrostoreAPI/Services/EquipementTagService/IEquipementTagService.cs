using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EquipementTagService;

public interface IEquipementTagService
{
    public Task<PaginatedResponseDto<ReadExtendedEquipementTagDto>> GetEquipementsTagsByEquipementId(int equipementId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedEquipementTagDto>> GetEquipementsTagsByTagId(int tagId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedEquipementTagDto> GetEquipementTagById(int equipementId, int tagId, List<string>? expand = null);

    public Task<ReadEquipementTagDto> CreateEquipementTag(CreateEquipementTagDto equipementTagDto);

    public Task<ReadBulkEquipementTagDto> CreateBulkEquipementTag(List<CreateEquipementTagDto> equipementTagBulkDto);

    public Task DeleteEquipementTag(int equipementId, int tagId);

    public Task<ReadBulkEquipementTagDto> DeleteBulkEquipementTag(List<CreateEquipementTagDto> equipementTagBulkDto);
}
