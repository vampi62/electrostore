using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EquipementBoxService;

public interface IEquipementBoxService
{
    public Task<PaginatedResponseDto<ReadExtendedEquipementBoxDto>> GetEquipementsBoxsByBoxId(int boxId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedEquipementBoxDto>> GetEquipementsBoxsByEquipementId(int equipementId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedEquipementBoxDto> GetEquipementBoxById(int equipementId, int boxId, List<string>? expand = null);

    public Task<ReadEquipementBoxDto> CreateEquipementBox(CreateEquipementBoxDto equipementBoxDto);

    public Task DeleteEquipementBox(int equipementId, int boxId);

    public Task CheckIfStoreExists(int storeId, int boxId);
}
