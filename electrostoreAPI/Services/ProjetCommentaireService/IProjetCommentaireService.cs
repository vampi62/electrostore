using electrostore.Dto;
using Microsoft.AspNetCore.Mvc;

namespace electrostore.Services.ProjetCommentaireService;

public interface IProjetCommentaireService
{
    public Task<ActionResult<IEnumerable<ReadProjetCommentaireDto>>> GetProjetCommentairesByProjetId(int projetId, int limit = 100, int offset = 0);

    public Task<ActionResult<IEnumerable<ReadProjetCommentaireDto>>> GetProjetCommentairesByUserId(int userId, int limit = 100, int offset = 0);

    public Task<ActionResult<ReadProjetCommentaireDto>> GetProjetCommentairesByCommentaireId(int id, int? userId = null, int? projetId = null);

    public Task<ActionResult<ReadProjetCommentaireDto>> CreateProjetCommentaire(CreateProjetCommentaireDto projetCommentaireDto);

    public Task<ActionResult<ReadProjetCommentaireDto>> UpdateProjetCommentaire(int id, UpdateProjetCommentaireDto projetCommentaireDto, int? userId = null, int? projetId = null);

    public Task<IActionResult> DeleteProjetCommentaire(int id, int? userId = null, int? projetId = null);
}