using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EquipementStatusService;

public interface IEquipementStatusService
{
    public Task<PaginatedResponseDto<ReadExtendedEquipementStatusDto>> GetEquipementStatusByEquipementId(int equipementId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadExtendedEquipementStatusDto> GetEquipementStatusById(int id, int? equipementId = null);

    public Task<ReadEquipementStatusDto> CreateEquipementStatus(CreateEquipementStatusDto equipementStatusDto);
}
