using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EquipementMaintenanceService;

public interface IEquipementMaintenanceService
{
    public Task<PaginatedResponseDto<ReadExtendedEquipementMaintenanceDto>> GetEquipementsMaintenancesByEquipementId(int equipementId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedEquipementMaintenanceDto> GetEquipementMaintenanceById(int id, int? equipementId = null, List<string>? expand = null);

    public Task<ReadEquipementMaintenanceDto> CreateEquipementMaintenance(CreateEquipementMaintenanceDto equipementMaintenanceDto);

    public Task<ReadEquipementMaintenanceDto> UpdateEquipementMaintenance(int id, UpdateEquipementMaintenanceDto equipementMaintenanceDto, int? equipementId = null);

    public Task DeleteEquipementMaintenance(int id, int? equipementId = null);
}
