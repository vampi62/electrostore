using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.EquipementCommentService;

public interface IEquipementCommentService
{
    public Task<PaginatedResponseDto<ReadExtendedEquipementCommentDto>> GetEquipementsCommentsByEquipementId(int equipementId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedEquipementCommentDto>> GetEquipementsCommentsByUserId(int userId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedEquipementCommentDto> GetEquipementCommentById(int id, int? userId = null, int? equipementId = null, List<string>? expand = null);

    public Task<ReadEquipementCommentDto> CreateComment(CreateEquipementCommentDto equipementCommentDto);

    public Task<ReadEquipementCommentDto> UpdateComment(int id, UpdateEquipementCommentDto equipementCommentDto, int? userId = null, int? equipementId = null);

    public Task DeleteComment(int id, int? userId = null, int? equipementId = null);
}
