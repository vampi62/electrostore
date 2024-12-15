using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandCommentaireService;

public interface ICommandCommentaireService
{
    public Task<IEnumerable<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetCommandsCommentairesCountByCommandId(int CommandId);

    public Task<IEnumerable<ReadExtendedCommandCommentaireDto>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0, List<string>? expand = null);

    public Task<int> GetCommandsCommentairesCountByUserId(int userId);

    public Task<ReadExtendedCommandCommentaireDto> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null, List<string>? expand = null);

    public Task<ReadCommandCommentaireDto> CreateCommentaire(CreateCommandCommentaireDto commandCommentaireDto);

    public Task<ReadCommandCommentaireDto> UpdateCommentaire(int id, UpdateCommandCommentaireDto commandCommentaireDto, int? userId = null, int? CommandId = null);

    public Task DeleteCommentaire(int id, int? userId = null, int? CommandId = null);
}