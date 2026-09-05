using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EquipementDocumentService;

public interface IEquipementDocumentService
{
    public Task<PaginatedResponseDto<ReadEquipementDocumentDto>> GetEquipementsDocumentsByEquipementId(int equipementId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null);

    public Task<ReadEquipementDocumentDto> GetEquipementDocumentById(int id, int? equipementId = null);

    public Task<ReadEquipementDocumentDto> CreateEquipementDocument(CreateEquipementDocumentDto equipementDocumentDto);

    public Task<ReadEquipementDocumentDto> UpdateEquipementDocument(int id, UpdateEquipementDocumentDto equipementDocumentDto, int? equipementId = null);

    public Task DeleteEquipementDocument(int id, int? equipementId = null);
}
