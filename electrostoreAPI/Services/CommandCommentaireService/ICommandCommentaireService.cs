using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.CommandCommentaireService;

public interface ICommandCommentaireService
{
    public Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByCommandId(int CommandId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadCommandCommentaireDto>>> GetCommandsCommentairesByUserId(int userId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadCommandCommentaireDto>> GetCommandsCommentaireById(int id, int? userId = null, int? CommandId = null);

    public Task<ActionResult<ReadCommandCommentaireDto>> CreateCommentaire(CreateCommandCommentaireDto commandCommentaireDto);

    public Task<ActionResult<ReadCommandCommentaireDto>> UpdateCommentaire(int id, UpdateCommandCommentaireDto commandCommentaireDto, int? userId = null, int? CommandId = null);

    public Task<IActionResult> DeleteCommentaire(int id, int? userId = null, int? CommandId = null);
}