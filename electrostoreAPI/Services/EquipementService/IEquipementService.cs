using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EquipementService;

public interface IEquipementService
{
    public Task<PaginatedResponseDto<ReadExtendedEquipementDto>> GetEquipements(int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null, List<int>? idResearch = null);

    public Task<ReadExtendedEquipementDto> GetEquipementById(int id, List<string>? expand = null);

    public Task<ReadEquipementDto> CreateEquipement(CreateEquipementDto equipementDto);

    public Task<ReadEquipementDto> UpdateEquipement(int id, UpdateEquipementDto equipementDto);

    public Task DeleteEquipement(int id);
}
