using ElectrostoreAPI.Dto;

namespace ElectrostoreAPI.Services.CommandCommentService;

public interface ICommandCommentService
{
    public Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentsByCommandId(int CommandId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<PaginatedResponseDto<ReadExtendedCommandCommentDto>> GetCommandsCommentsByUserId(int userId, int limit = 100, int offset = 0, List<FilterDto>? rsql = null, SorterDto? sort = null, List<string>? expand = null);

    public Task<ReadExtendedCommandCommentDto> GetCommandsCommentById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null);

    public Task<ReadCommandCommentDto> CreateComment(CreateCommandCommentDto commandCommentDto);

    public Task<ReadCommandCommentDto> UpdateComment(int id, UpdateCommandCommentDto commandCommentDto, int? userId = null, int? CommandId = null);

    public Task DeleteComment(int id, int? userId = null, int? CommandId = null);
}