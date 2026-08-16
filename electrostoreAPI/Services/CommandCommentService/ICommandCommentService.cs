using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.CommandCommentService;

public interface ICommandCommentService
{
    public Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedCommandCommentDto> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null);

    public Task<ReadCommandCommentDto> CreateCommentaire(CreateCommandCommentDto commandCommentaireDto);

    public Task<ReadCommandCommentDto> UpdateCommentaire(int id, UpdateCommandCommentDto commandCommentaireDto, int? userId = null, int? CommandId = null);

    public Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null);
}