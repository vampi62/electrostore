using electrostore.Dto;

namespace electrostore.Services.CommandCommentaireService;

public interface ICommandCommentaireService
{
    public Task<PaginatedResponseDto<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedCommandCommentaireDto> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null);

    public Task<ReadCommandCommentaireDto> CreateCommentaire(CreateCommandCommentaireDto commandCommentaireDto);

    public Task<ReadCommandCommentaireDto> UpdateCommentaire(int id, UpdateCommandCommentaireDto commandCommentaireDto, int? userId = null, int? CommandId = null);

    public Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null);
}